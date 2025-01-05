# pip install einops timm
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install psutil
# pip install flash-attn --no-build-isolation
import requests
import torch
from PIL import Image
from transformers import AutoProcessor, AutoModelForCausalLM 
import time

device = "cuda:0" if torch.cuda.is_available() else "cpu"
torch_dtype = torch.float16 if torch.cuda.is_available() else torch.float32

model_id = "microsoft/Florence-2-base"
#model_id = "microsoft/Florence-2-base-ft"
#model_id = "microsoft/Florence-2-large"
#model_id = "microsoft/Florence-2-large-ft"
print("start:", time.strftime("%H:%M:%S", time.localtime()))
print("calling AutoModelForCausalLM.from_pretrained...", time.strftime("%H:%M:%S", time.localtime()))
model = AutoModelForCausalLM.from_pretrained(model_id, torch_dtype=torch_dtype, trust_remote_code=True).to(device)

print("calling AutoProcessor.from_pretrained...", time.strftime("%H:%M:%S", time.localtime()))
processor = AutoProcessor.from_pretrained(model_id, trust_remote_code=True)

# https://huggingface.co/microsoft/Florence-2-large/blob/main/sample_inference.ipynb
# tasks: <CAPTION>, <DETAILED_CAPTION>, <MORE_DETAILED_CAPTION>, <OD>, <DENSE_REGION_CAPTION>, <REGION_PROPOSAL>, <OCR>, <OCR_WITH_REGION>
# tasks require additional input: <CAPTION_TO_PHRASE_GROUNDING>, <REFERRING_EXPRESSION_SEGMENTATION>, <REGION_TO_SEGMENTATION>, <OPEN_VOCABULARY_DETECTION>, <REGION_TO_CATEGORY>, <REGION_TO_DESCRIPTION>
# OD results format: {'<OD>': { 'bboxes': [[x1, y1, x2, y2], ...], 'labels': ['label1', 'label2', ...] } }
# Dense region caption results format: {'<DENSE_REGION_CAPTION>': {'bboxes': [[x1, y1, x2, y2], ...], 'labels': ['label1', 'label2', ...]}}
# Region proposal results format: {'<REGION_PROPOSAL>' : {'bboxes': [[x1, y1, x2, y2], ...], 'labels': ['', '', ...]}}
# Phrase grounding results format: {'<CAPTION_TO_PHRASE_GROUNDING>': {'bboxes': [[x1, y1, x2, y2], ...], 'labels': ['', '', ...]}}
# Referring expression segmentation results format: {'<REFERRING_EXPRESSION_SEGMENTATION>': {'Polygons': [[[polygon]], ...], 'labels': ['', '', ...]}}, one object is represented by a list of polygons. each polygon is [x1, y1, x2, y2, ..., xn, yn]
# <REGION_TO_SEGMENTATION> with additional region as inputs, format is '<loc_x1><loc_y1><loc_x2><loc_y2>', [x1, y1, x2, y2] is the quantized corrdinates in [0, 999].
# <OPEN_VOCABULARY_DETECTION> can detect both objects and ocr texts.
#   results format:
#   { '<OPEN_VOCABULARY_DETECTION>': {'bboxes': [[x1, y1, x2, y2], [x1, y1, x2, y2], ...]], 'bboxes_labels': ['label_1', 'label_2', ..], 'polygons': [[[x1, y1, x2, y2, ..., xn, yn], [x1, y1, ..., xn, yn]], ...], 'polygons_labels': ['label_1', 'label_2', ...] }}


# prompt = "<OD>"
#task = "<OD>"
prompt = "<OCR>"
task = "<OCR>"

# url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/car.jpg?download=true"
# image = Image.open(requests.get(url, stream=True).raw)
# image = Image.open("..\\..\\images\\MYDL1_s.jpg")
image = Image.open("..\\images\\handwritten1.jpg")
# image = Image.open("..\\images\\CSDEMOBANK.jpg")

print("calling processor...", time.strftime("%H:%M:%S", time.localtime()))
inputs = processor(text=prompt, images=image, return_tensors="pt").to(device, torch_dtype)

print("calling model.generate...", time.strftime("%H:%M:%S", time.localtime()))
generated_ids = model.generate(
    input_ids=inputs["input_ids"],
    pixel_values=inputs["pixel_values"],
    #max_new_tokens=1024,
    max_new_tokens=4096,
    do_sample=False,
    num_beams=3,
)

print("calling processor.batch_decode...", time.strftime("%H:%M:%S", time.localtime()))
generated_text = processor.batch_decode(generated_ids, skip_special_tokens=False)[0]

print(generated_text)

print("calling processor.post_process_generation...", time.strftime("%H:%M:%S", time.localtime()))
parsed_answer = processor.post_process_generation(generated_text, task, image_size=(image.width, image.height))

print("finished: ", time.strftime("%H:%M:%S", time.localtime()))
print(parsed_answer)
