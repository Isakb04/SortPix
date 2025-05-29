import json
from torchvision.models import MobileNet_V2_Weights
from ultralytics import YOLO
import torch
from torchvision import models, transforms
from PIL import Image
import os
import importlib
import numpy as np
from multiprocessing import Pool
from sklearn.metrics import f1_score

# Load YOLOv8 model
yolo_model = YOLO('yolov8s.pt')

# Load MobileNetV2 model
mobilenet = models.mobilenet_v2(weights=MobileNet_V2_Weights.IMAGENET1K_V1)
mobilenet.eval()

# Define preprocessing for MobileNet
preprocess = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
])

# Load YOLO class labels
with open(os.path.join(os.path.dirname(__file__), 'coco.names')) as f:
    yolo_class_labels = [line.strip() for line in f]

# Load MobileNet class labels
with open(os.path.join(os.path.dirname(__file__), 'imagenet_classes.txt')) as f:
    class_labels = {i: line.strip() for i, line in enumerate(f)}

# Parse NoTag.json into a Python list with error handling
try:
    no_tag_path = os.path.join(os.path.dirname(__file__), 'NoTag.json')
    with open(no_tag_path, 'r', encoding='utf-8-sig') as f:
        print(f"Loading NoTag.json")
        no_tag_data = json.load(f)
        no_tag_images = [item["ImageName"] for item in no_tag_data.get("NoTagImages", [])]
        print(f"Loaded {len(no_tag_images)} images from NoTag.json")
except (json.JSONDecodeError, ValueError, KeyError, FileNotFoundError) as e:
    print(f"Error loading NoTag.json: {e}. Defaulting to an empty list.")
    no_tag_images = []

# Parse ManualTag.json into a dictionary with error handling
try:
    manual_tag_path = os.path.join(os.path.dirname(__file__), 'ManualTag.json')
    with open(manual_tag_path, 'r', encoding='utf-8-sig') as f:
        print(f"Loading ManualTag.json")
        manual_tag_data = json.load(f)
        manual_tag_images = {item["ImageName"]: item["Tags"] for item in manual_tag_data.get("ManualTagImages", [])}
        print(f"Loaded {len(manual_tag_images)} images from ManualTag.json")
except (json.JSONDecodeError, ValueError, KeyError) as e:
    print(f"Error loading ManualTag.json: {e}. Defaulting to an empty dictionary.")
    manual_tag_images = {}

# Input and output directories
image_dir = os.path.join(os.path.dirname(__file__), 'images1')
# image_dir = os.path.join(os.path.expanduser('~'), 'Pictures')    

output_dir = os.path.join(os.path.dirname(__file__), 'Processed_Images')
os.makedirs(output_dir, exist_ok=True)

confidence_file_path = os.path.join(os.path.dirname(__file__), 'confidence_scores.txt')

def get_predicted_tags_and_confidences(image_path):
    """Return predicted tags and confidences for YOLO and MobileNet."""
    yolo_tags = set()
    yolo_confidences = []
    results = yolo_model(image_path)
    for result in results:
        for cls_id, conf in zip(result.boxes.cls.cpu().numpy(), result.boxes.conf.cpu().numpy()):
            label = yolo_class_labels[int(cls_id)]
            yolo_tags.add(label)
            yolo_confidences.append((label, float(conf)))
    with torch.no_grad():
        output = mobilenet(preprocess(Image.open(image_path)).unsqueeze(0))
        predicted_class = output.argmax(dim=1).item()
        mobilenet_label = class_labels.get(predicted_class, f"Unknown({predicted_class})")
        mobilenet_conf = float(torch.softmax(output, dim=1)[0, predicted_class].item())
    return yolo_tags, yolo_confidences, mobilenet_label, mobilenet_conf

# Function to process a single image, now receives full image path and relative path
def process_image(args):
    image_path, rel_path = args
    image_name = os.path.basename(image_path)

    # Skip processing if the image is in NoTag.json
    if image_name in no_tag_images:
        print(f"Skipping {image_name} as it is in NoTag.json")
        return

    # Check if the image is in ManualTag.json
    if image_name in manual_tag_images:
        tags = manual_tag_images[image_name]
        for tag in tags:
            manual_tag_folder = os.path.join(output_dir, tag, os.path.dirname(rel_path))
            os.makedirs(manual_tag_folder, exist_ok=True)
            manual_shortcut_path = os.path.join(manual_tag_folder, image_name)
            if not os.path.exists(manual_shortcut_path):
                os.symlink(os.path.abspath(image_path), manual_shortcut_path)
        # Write confidences for manual images too
        yolo_tags, yolo_confidences, mobilenet_label, mobilenet_conf = get_predicted_tags_and_confidences(image_path)
        with open(confidence_file_path, 'a', encoding='utf-8') as cf:
            cf.write(f"{image_name}:\n")
            if yolo_confidences:
                for label, conf in yolo_confidences:
                    cf.write(f"  YOLO: {label} (confidence: {conf:.4f})\n")
            else:
                cf.write("  YOLO: No detections\n")
            cf.write(f"  MobileNet: {mobilenet_label} (confidence: {mobilenet_conf:.4f})\n\n")
        print(f"Assigned manual tags {tags} to {image_name}")
        return

    # Perform inference with YOLO
    results = yolo_model(image_path)

    # Process YOLO detected objects
    yolo_confidences = []
    for result in results:
        for box, cls_id, conf in zip(result.boxes.xyxy.cpu().numpy(), result.boxes.cls.cpu().numpy(), result.boxes.conf.cpu().numpy()):
            yolo_class_label = yolo_class_labels[int(cls_id)]
            yolo_tag_folder = os.path.join(output_dir, yolo_class_label, os.path.dirname(rel_path))
            os.makedirs(yolo_tag_folder, exist_ok=True)
            yolo_shortcut_path = os.path.join(yolo_tag_folder, image_name)
            if not os.path.exists(yolo_shortcut_path):
                os.symlink(os.path.abspath(image_path), yolo_shortcut_path)
            yolo_confidences.append((yolo_class_label, float(conf)))

    # Perform MobileNet classification
    with torch.no_grad():
        output = mobilenet(preprocess(Image.open(image_path)).unsqueeze(0))
        predicted_class = output.argmax(dim=1).item()
    mobilenet_class_label = class_labels.get(predicted_class, f"Unknown({predicted_class})")
    mobilenet_confidence = float(torch.softmax(output, dim=1)[0, predicted_class].item())
    mobilenet_tag_folder = os.path.join(output_dir, mobilenet_class_label, os.path.dirname(rel_path))
    os.makedirs(mobilenet_tag_folder, exist_ok=True)
    mobilenet_shortcut_path = os.path.join(mobilenet_tag_folder, image_name)
    if not os.path.exists(mobilenet_shortcut_path):
        os.symlink(os.path.abspath(image_path), mobilenet_shortcut_path)

    # Write confidence scores to file
    with open(confidence_file_path, 'a', encoding='utf-8') as cf:
        cf.write(f"{image_name}:\n")
        if yolo_confidences:
            for label, conf in yolo_confidences:
                cf.write(f"  YOLO: {label} (confidence: {conf:.4f})\n")
        else:
            cf.write("  YOLO: No detections\n")
        cf.write(f"  MobileNet: {mobilenet_class_label} (confidence: {mobilenet_confidence:.4f})\n\n")

# F1 Score Calculation for manually tagged images
def compute_f1_for_manual_tags():
    y_true = []
    y_pred = []
    all_tags = set()
    # Collect all unique tags from manual tags and predictions
    for image_name, tags in manual_tag_images.items():
        all_tags.update(tags)
        image_path = None
        for root, dirs, files in os.walk(image_dir):
            if image_name in files:
                image_path = os.path.join(root, image_name)
                break
        if image_path:
            yolo_tags, _, mobilenet_label, _ = get_predicted_tags_and_confidences(image_path)
            pred_tags = set(yolo_tags)
            pred_tags.add(mobilenet_label)
            all_tags.update(pred_tags)
    all_tags = sorted(list(all_tags))
    tag_to_idx = {tag: i for i, tag in enumerate(all_tags)}

    for image_name, tags in manual_tag_images.items():
        image_path = None
        for root, dirs, files in os.walk(image_dir):
            if image_name in files:
                image_path = os.path.join(root, image_name)
                break
        if image_path:
            yolo_tags, _, mobilenet_label, _ = get_predicted_tags_and_confidences(image_path)
            pred_tags = set(yolo_tags)
            pred_tags.add(mobilenet_label)
            true_vec = [1 if tag in tags else 0 for tag in all_tags]
            pred_vec = [1 if tag in pred_tags else 0 for tag in all_tags]
            y_true.append(true_vec)
            y_pred.append(pred_vec)
    if y_true and y_pred:
        f1 = f1_score(y_true, y_pred, average='macro', zero_division=0)
        print(f"Macro F1 score for manually tagged images: {f1:.4f}")
        with open(confidence_file_path, 'a', encoding='utf-8') as cf:
            cf.write(f"Macro F1 score for manually tagged images: {f1:.4f}\n")
    else:
        print("No manually tagged images found for F1 score calculation.")

if __name__ == '__main__':
    # Clear the confidence file at the start
    open(confidence_file_path, 'w').close()
    image_files = []
    for root, dirs, files in os.walk(image_dir):
        for f in files:
            if f.lower().endswith(('.jpg', '.png', '.jpeg')):
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, image_dir)
                image_files.append((full_path, rel_path))
    with Pool() as pool:
        pool.map(process_image, image_files)
    compute_f1_for_manual_tags()
