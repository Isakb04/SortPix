from torchvision.models import MobileNet_V2_Weights
from ultralytics import YOLO
import torch
from torchvision import models, transforms
from PIL import Image
import os
from multiprocessing import Pool

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

# Input and output directories
image_dir = os.path.join(os.path.dirname(__file__), 'images1')
output_dir = os.path.join(os.path.dirname(__file__), 'Processed_Images')
os.makedirs(output_dir, exist_ok=True)

# Function to process a single image
def process_image(image_name):
    image_path = os.path.join(image_dir, image_name)

    # Perform inference with YOLO
    results = yolo_model(image_path)

    # Process YOLO detected objects
    for result in results:
        for box, cls_id in zip(result.boxes.xyxy.cpu().numpy(), result.boxes.cls.cpu().numpy()):
            yolo_class_label = yolo_class_labels[int(cls_id)]
            yolo_tag_folder = os.path.join(output_dir, yolo_class_label)
            os.makedirs(yolo_tag_folder, exist_ok=True)
            yolo_shortcut_path = os.path.join(yolo_tag_folder, image_name)
            if not os.path.exists(yolo_shortcut_path):
                os.symlink(os.path.abspath(image_path), yolo_shortcut_path)

    # Perform MobileNet classification
    with torch.no_grad():
        output = mobilenet(preprocess(Image.open(image_path)).unsqueeze(0))
        predicted_class = output.argmax(dim=1).item()
    mobilenet_class_label = class_labels.get(predicted_class, f"Unknown({predicted_class})")
    mobilenet_tag_folder = os.path.join(output_dir, mobilenet_class_label)
    os.makedirs(mobilenet_tag_folder, exist_ok=True)
    mobilenet_shortcut_path = os.path.join(mobilenet_tag_folder, image_name)
    if not os.path.exists(mobilenet_shortcut_path):
        os.symlink(os.path.abspath(image_path), mobilenet_shortcut_path)

# Use multiprocessing to process images in parallel
if __name__ == '__main__':
    image_files = [f for f in os.listdir(image_dir) if f.endswith(('.jpg', '.png', '.jpeg'))]
    with Pool() as pool:
        pool.map(process_image, image_files)