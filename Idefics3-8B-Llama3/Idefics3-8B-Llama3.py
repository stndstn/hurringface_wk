# pip install requests Pillow
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124

import requests
import torch
from PIL import Image
from io import BytesIO
import time

# https://huggingface.co/HuggingFaceM4/Idefics3-8B-Llama3/discussions/1
# https://github.com/huggingface/transformers/pull/32473
# https://github.com/andimarafioti/transformers/tree/idefics3
# pip install git+https://github.com/andimarafioti/transformers.git@idefics3
from transformers import AutoProcessor, AutoModelForVision2Seq
from transformers.image_utils import load_image

# print time hh:mm:ss
now = time.localtime()
print('start ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

DEVICE = "cuda:0"

# Note that passing the image urls (instead of the actual pil images) to the processor is also possible
#image1 = load_image("https://cdn.britannica.com/61/93061-050-99147DCE/Statue-of-Liberty-Island-New-York-Bay.jpg")
#image2 = load_image("https://cdn.britannica.com/59/94459-050-DBA42467/Skyline-Chicago.jpg")
#image3 = load_image("https://cdn.britannica.com/68/170868-050-8DDE8263/Golden-Gate-Bridge-San-Francisco.jpg")
image = Image.open("..//images//MYDL1_s.jpg")

now = time.localtime()
print('calling AutoProcessor.from_pretrained... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

processor = AutoProcessor.from_pretrained("HuggingFaceM4/Idefics3-8B-Llama3")

now = time.localtime()
print('calling AutoModelForVision2Seq.from_pretrained... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

model = AutoModelForVision2Seq.from_pretrained(
    "HuggingFaceM4/Idefics3-8B-Llama3", torch_dtype=torch.bfloat16
).to(DEVICE)

# Create inputs
'''
messages = [
    {
        "role": "user",
        "content": [
            {"type": "image"},
            {"type": "text", "text": "What do we see in this image?"},
        ]
    },
    {
        "role": "assistant",
        "content": [
            {"type": "text", "text": "In this image, we can see the city of New York, and more specifically the Statue of Liberty."},
        ]
    },
    {
        "role": "user",
        "content": [
            {"type": "image"},
            {"type": "text", "text": "And how about this image?"},
        ]
    },       
]
'''
messages = [
    {
        "role": "user",
        "content": [
            {"type": "image"},
            {"type": "text", "text": "This is Mayaisian driving license. What is the name and ID number of license holder?"},
            #{"type": "text", "text": "List all lines of text in this image in json format."},          
            #{"type": "text", "text": "List all lines of text in this image."},          
        ]
    },
]
'''
calling model.generate()...  23 : 27 : 34
calling processor.batch_decode()...  23 : 35 : 2
finished  23 : 35 : 2
['User:<image>List all lines of text in this image in json format.\n
Assistant: {\n    "id": "TAKUMI TATEISHI",\n    "nationality": "JPN",\n    "passport_number": "T2114505IJ",\n    "age": "B2 D",\n    "date_of_birth": "19/09/2016",\n    "expiry_date": "18/04/2021",\n    "address": "42-12F City Tower, Jln Alor Bkt Bintang, 50200 Kuala Lumpur, Wilayah Persekutuan Kuala Lumpur",\n    "photo": "A photo of a man"\n}']

calling model.generate()...  23 : 39 : 37
calling processor.batch_decode()...  0 : 0 : 3
finished  0 : 0 : 3
['User:<image>List all lines of text in this image.\nAssistant: ### Image Description\n\nThe image depicts a Malaysian driving license. The driving license is rectangular in shape and has a white background with a blue border. The top left corner features the emblem of Malaysia, which includes a crescent moon and a star, symbolizing the country\'s national identity. The text "LESEN MEMANDU" is written above the emblem, indicating that this is a driving license. Below the emblem, the word "MALAYSIA" is prominently displayed in blue capital letters, signifying the country of issuance.\n\nIn the center of the license, there is a photograph of a person, presumably the license holder. The person in the photo has short black hair and is wearing a white shirt. The photo is positioned slightly off-center to the left.\n\nBelow the photo, there is a section with personal information. The name "TAKUMI TATEISHI" is written in black capital letters. The text "JPN" is also present, which likely stands for "Jabatan Pendaftaran Negara," the National Registration Department of Malaysia. The license number "T2114505J" is displayed, followed by "B2 D," which indicates the type of license and possibly the class of vehicle it allows the holder to drive.\n\nThe validity of the license is specified as "19/09/2016 - 18/04/2021," indicating the start and end dates of the license\'s validity period. The address "42-12F CITY TOWER" is listed, followed by "JLN ALOR BINTANG," which is a street address in Kuala Lumpur, Malaysia. The postal code "50200" is also provided, which is the postal code for the area.\n\nThe bottom right corner of the license contains a small floral emblem, possibly a national symbol or a logo related to the issuing authority.\n\n### Analysis and Description\n\nThe driving license is a legal document issued by the Malaysian government to an individual named Takumi Tateishi. The license is valid for a specific period, from September 19, 2016, to April 18, 2021. The holder is allowed to drive a B2 class vehicle, which typically refers to a motorcycle with an engine capacity of up to 250cc. The address listed on the license is a commercial building in the heart of Kuala Lumpur, indicating that the holder may reside or work in this area.\n\nThe presence of the national emblem and the text "LESEN MEMANDU']
'''
now = time.localtime()
print('calling processor.apply_chat_template... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

prompt = processor.apply_chat_template(messages, add_generation_prompt=True)

now = time.localtime()
print('calling processor()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

inputs = processor(text=prompt, images=[image], return_tensors="pt")
inputs = {k: v.to(DEVICE) for k, v in inputs.items()}


# Generate
now = time.localtime()
print('calling model.generate()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

generated_ids = model.generate(**inputs, max_new_tokens=500)

now = time.localtime()
print('calling processor.batch_decode()... ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

generated_texts = processor.batch_decode(generated_ids, skip_special_tokens=True)

now = time.localtime()
print('finished ', now.tm_hour, ':', now.tm_min, ':', now.tm_sec)

print(generated_texts)