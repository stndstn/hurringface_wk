# pip install einops timm
# pip install "numpy<2.0"
# pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install psutil
# (python -m pip install wheel) 
# pip install flash-attn --no-build-isolation
# pip install transformers
'''
Florence2LanguageForConditionalGeneration has generative capabilities, as prepare_inputs_for_generation is explicitly overwritten. 
However, it doesn't directly inherit from GenerationMixin. 
From 👉v4.50👈 onwards, PreTrainedModel will NOT inherit from GenerationMixin, and this model will lose the ability to call generate and other related functions.
If you're using trust_remote_code=True, you can get rid of this warning by loading the model with an auto class. See https://huggingface.co/docs/transformers/en/model_doc/auto#auto-classes
If you are the owner of the model architecture code, please modify your model class such that it inherits from GenerationMixin (after PreTrainedModel, otherwise you'll get an exception).
'''
# (install rust compiler, then 'pip install "transformers==4.44.2"'
# pip install flask

# RMKS: if install by requirements.txt does not to work. delete all cache and re-install with pip manually 
## pip freeze > requirements.txt
## pip install -r requirements.txt


import requests
import torch

from PIL import Image
from transformers import AutoProcessor, AutoModelForCausalLM 

print("Florence-2-base...")
device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"deivice: {device}")
torch_dtype = torch.float16 if torch.cuda.is_available() else torch.float32
print(f"torch_dtype: {torch_dtype}")

model = AutoModelForCausalLM.from_pretrained("microsoft/Florence-2-base", torch_dtype=torch_dtype, trust_remote_code=True).to(device)
#print(f"model: {model}")
processor = AutoProcessor.from_pretrained("microsoft/Florence-2-base", trust_remote_code=True)
#print(f"processor: {processor}")

def getDevice():
    return device

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

promptOcr = "<OCR>"
taskOcr = "<OCR>"
promptOcrWithRegion = "<OCR_WITH_REGION>"
taskOcrWithRegion = "<OCR_WITH_REGION>"

def ocr(image):
    print(f"ocr image: {image}")
    # url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/car.jpg?download=true"
    # image = Image.open(requests.get(url, stream=True).raw)
    # image = Image.open("..\\..\\images\\MYDL1_s.jpg")
    # image = Image.open("..\\..\\images\\handwritten1.jpg")

    inputs = processor(text=promptOcr, images=image, return_tensors="pt").to(device, torch_dtype)
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

    parsed_answer = processor.post_process_generation(generated_text, taskOcr, image_size=(image.width, image.height))
    print(f"ocr parsed_answer: {parsed_answer}")
    return parsed_answer

def ocrWithRegion(image):
    print(f"ocr image: {image}")
    # url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/car.jpg?download=true"
    # image = Image.open(requests.get(url, stream=True).raw)
    # image = Image.open("..\\..\\images\\MYDL1_s.jpg")
    # image = Image.open("..\\..\\images\\handwritten1.jpg")

    inputs = processor(text=promptOcrWithRegion, images=image, return_tensors="pt").to(device, torch_dtype)
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

    parsed_answer = processor.post_process_generation(generated_text, taskOcrWithRegion, image_size=(image.width, image.height))
    print(f"ocr parsed_answer: {parsed_answer}")
    return parsed_answer

'''
image = Image.open("..\\..\\images\\handwritten1.jpg")
ocr_answer = ocr(image)
print(ocr_answer['<OCR>'])
# {'<OCR>': "Frank-Sweetie I amokay. I'm wl myoffice overbyThe Lyndon B. Johnsonmemorial"}
'''

