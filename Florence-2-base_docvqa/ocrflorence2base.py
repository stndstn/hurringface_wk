# pip install einops timm
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install psutil
# pip install flash-attn --no-build-isolation
# pip install transformers
# pip freeze > requirements.txt
# pip install -r requirements.txt

import requests
import torch

from PIL import Image
from transformers import AutoProcessor, AutoModelForCausalLM 

print("Florence-2-base...")
device = "cuda:0" if torch.cuda.is_available() else "cpu"
print(f"deivice: {device}")
torch_dtype = torch.float16 if torch.cuda.is_available() else torch.float32
print(f"torch_dtype: {torch_dtype}")

model = AutoModelForCausalLM.from_pretrained("microsoft/Florence-2-base", torch_dtype=torch_dtype, trust_remote_code=True).to(device)
print(f"model: {model}")
processor = AutoProcessor.from_pretrained("microsoft/Florence-2-base", trust_remote_code=True)
print(f"processor: {processor}")

# https://huggingface.co/microsoft/Florence-2-large/blob/main/sample_inference.ipynb
# Run pre-defined tasks without additional inputs
## Caption: <CAPTION>, <DETAILED_CAPTION>, <MORE_DETAILED_CAPTION>, 
## Object detection: <OD>, 
## Dense region caption: <DENSE_REGION_CAPTION>, 
## Region proposal: <REGION_PROPOSAL>, 
## ocr related tasks: <OCR>, <OCR_WITH_REGION>, 
# Run pre-defined tasks that requires additional inputs
## Phrase Grounding: <CAPTION_TO_PHRASE_GROUNDING>, 
## Referring expression segmentation: <REFERRING_EXPRESSION_SEGMENTATION>
## Region to segmentation: <REGION_TO_SEGMENTATION>, 
## Open vocabulary detection: <OPEN_VOCABULARY_DETECTION>, 
## Region to texts: <REGION_TO_CATEGORY>, <REGION_TO_DESCRIPTION>

prompt = "<OCR>"
task = "<OCR>"

def ocr(image):
    print(f"ocr image: {image}")
    # url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/car.jpg?download=true"
    # image = Image.open(requests.get(url, stream=True).raw)
    # image = Image.open("..\\..\\images\\MYDL1_s.jpg")
    # image = Image.open("..\\..\\images\\handwritten1.jpg")

    inputs = processor(text=prompt, images=image, return_tensors="pt").to(device, torch_dtype)
    print(f"ocr inputs: {inputs}")

    generated_ids = model.generate(
        input_ids=inputs["input_ids"],
        pixel_values=inputs["pixel_values"],
        max_new_tokens=1024,
        do_sample=False,
        num_beams=3,
    )
    print(f"ocr generated_ids: {generated_ids}")
    generated_text = processor.batch_decode(generated_ids, skip_special_tokens=False)[0]
    print(f"ocr generated_text: {generated_text}")

    parsed_answer = processor.post_process_generation(generated_text, task, image_size=(image.width, image.height))
    print(f"ocr parsed_answer: {parsed_answer}")
    return parsed_answer

'''
image = Image.open("..\\..\\images\\handwritten1.jpg")
ocr_answer = ocr(image)
print(ocr_answer['<OCR>'])
# {'<OCR>': "Frank-Sweetie I amokay. I'm wl myoffice overbyThe Lyndon B. Johnsonmemorial"}
'''

