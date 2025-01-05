# pip install accelerate
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124

import requests
from PIL import Image
import torch
from transformers import AutoProcessor, LlavaForConditionalGeneration
import time

# print time hh:mm:ss
now = time.localtime()
print('start ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

device = "cuda:0" if torch.cuda.is_available() else "cpu"
model_id = "llava-hf/llava-1.5-7b-hf"

now = time.localtime()
print('calling LlavaForConditionalGeneration.from_pretrained()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

model = LlavaForConditionalGeneration.from_pretrained(
    model_id, 
    torch_dtype=torch.float16, 
    low_cpu_mem_usage=True, 
).to(device)

now = time.localtime()
print('calling AutoProcessor.from_pretrained()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

processor = AutoProcessor.from_pretrained(model_id)

# Define a chat histiry and use `apply_chat_template` to get correctly formatted prompt
# Each value in "content" has to be a list of dicts with types ("text", "image") 
'''
conversation = [
    {

      "role": "user",
      "content": [
          {"type": "text", "text": "What are these?"},
          {"type": "image"},
        ],
    },
]
'''
'''
conversation = [
    {

      "role": "user",
      "content": [
          {"type": "text", "text": "This is Mayaisian driving license. What is the name of license holder?"},
          {"type": "image"},
        ],
    },
]
'''
conversation = [
    {
      "role": "user",
      "content": [
          #{"type": "text", "text": "What is the title of this form?"},
          #{"type": "text", "text": "What is the text filled in the box labeled 'Name (Last, Suffix, First, Middle)' in 'SECTION A PERSONAL INFORMATION'?"},
          {"type": "text", "text": "List all the information in 'SECTION A PERSONAL INFORMATION' in json format."},
          #{"type": "text", "text": "List all lines of text in this image in json format."},          
          {"type": "image"},
        ],
    },
]

now = time.localtime()
print('calling processor.apply_chat_template()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

prompt = processor.apply_chat_template(conversation, add_generation_prompt=True)

#image_file = "http://images.cocodataset.org/val2017/000000039769.jpg"
#raw_image = Image.open(requests.get(image_file, stream=True).raw)
# raw_image = Image.open("images/MYDL1_s.jpg")
raw_image = Image.open("..\\images\\CSDEMOBANK.jpg")
#raw_image = Image.open("..\\images\\MYDL2.png")

now = time.localtime()
print('calling processor.to()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

inputs = processor(images=[raw_image], text=prompt, return_tensors='pt').to(0, torch.float16)

now = time.localtime()
print('calling model.generate()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

output = model.generate(**inputs, max_new_tokens=200, do_sample=False)

now = time.localtime()
print('calling processor.decode()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

print(processor.decode(output[0][2:], skip_special_tokens=True))

now = time.localtime()
print('finished ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)
