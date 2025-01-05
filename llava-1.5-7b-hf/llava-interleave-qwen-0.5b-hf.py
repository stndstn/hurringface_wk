import requests
from PIL import Image

import torch
from transformers import AutoProcessor, LlavaForConditionalGeneration

import time

model_id = "llava-hf/llava-interleave-qwen-0.5b-hf"
model = LlavaForConditionalGeneration.from_pretrained(
    model_id, 
    torch_dtype=torch.float16, 
    low_cpu_mem_usage=True, 
).to(0)

processor = AutoProcessor.from_pretrained(model_id)
'''
# Define a chat history and use `apply_chat_template` to get correctly formatted prompt
# Each value in "content" has to be a list of dicts with types ("text", "image") 
conversation = [
    {
      "role": "user",
      "content": [
          {"type": "text", "text": "What are these?"},
          {"type": "image"},
        ],
    },
]
prompt = processor.apply_chat_template(conversation, add_generation_prompt=True)

image_file = "http://images.cocodataset.org/val2017/000000039769.jpg"
raw_image = Image.open(requests.get(image_file, stream=True).raw)
inputs = processor(images=raw_image, text=prompt, return_tensors='pt').to(0, torch.float16)
'''
image = Image.open("..\\images\\MYDL2.png")
conversation = [
    {
      "role": "user",
      "content": [
          #{"type": "text", "text": "What is the title of this form?"},
          #{"type": "text", "text": "What is the text filled in the box labeled 'Name (Last, Suffix, First, Middle)' in 'SECTION A PERSONAL INFORMATION'?"},
          #{"type": "text", "text": "List all the information in 'SECTION A PERSONAL INFORMATION' of this form."},
          #{"type": "text", "text": "List all the text in this document."},
          #{"type": "text", "text": "List all the information in this image."},
          #{"type": "text", "text": "This is Mayaisian driving license. What is the name and ID number of license holder?"},
          {"type": "text", "text": "List all lines of text in this image in json format."},          
          #{"type": "text", "text": "List all lines of text in this image."},          
          {"type": "image"},
        ],
    },
]
prompt = processor.apply_chat_template(conversation, add_generation_prompt=True)

print("calling processor...", time.strftime("%H:%M:%S", time.localtime()))
inputs = processor(images=image, text=prompt, return_tensors='pt').to(0, torch.float16)

print("calling model.generate...", time.strftime("%H:%M:%S", time.localtime()))
output = model.generate(**inputs, max_new_tokens=200, do_sample=False)
print("calling processor.decode...", time.strftime("%H:%M:%S", time.localtime()))
print(processor.decode(output[0][2:], skip_special_tokens=True))


'''
# image = Image.open("..\\images\\CSDEMOBANK.jpg")
image = Image.open("..\\images\\MYDL2.png")
conversation = [
    {
      "role": "user",
      "content": [
          #{"type": "text", "text": "What is the title of this form?"},
          #{"type": "text", "text": "What is the text filled in the box labeled 'Name (Last, Suffix, First, Middle)' in 'SECTION A PERSONAL INFORMATION'?"},
          #{"type": "text", "text": "List all the information in 'SECTION A PERSONAL INFORMATION' of this form."},
          #{"type": "text", "text": "List all the text in this document."},
          #{"type": "text", "text": "List all the information in this image."},
          #{"type": "text", "text": "This is Mayaisian driving license. What is the name and ID number of license holder?"},
          {"type": "text", "text": "List all lines of text in this image in json format."},          
          #{"type": "text", "text": "List all lines of text in this image."},          
          {"type": "image"},
        ],
    },
]

print("calling processor.apply_chat_template...", time.strftime("%H:%M:%S", time.localtime()))
prompt = processor.apply_chat_template(conversation, add_generation_prompt=False)

print("calling pipe...", time.strftime("%H:%M:%S", time.localtime()))
outputs = pipe(image, prompt=prompt, generate_kwargs={"max_new_tokens": 500})

print("finish: ", time.strftime("%H:%M:%S", time.localtime()))
print(outputs)
'''
