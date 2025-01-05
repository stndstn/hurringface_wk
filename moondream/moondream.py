
# https://huggingface.co/vikhyatk/moondream2
# https://github.com/vikhyat/moondream
# https://huggingface.co/docs/transformers/perf_infer_gpu_one#flashattention-2 

#pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
#pip install transformers einops Pillow
#pip install "numpy<2.0"
#pip install flash-attn --no-build-isolation

from transformers import AutoModelForCausalLM, AutoTokenizer
from PIL import Image
import torch
import time

print("start: ", time.strftime("%H:%M:%S", time.localtime()))

model_id = "vikhyatk/moondream2"
revision = "2024-08-26"

print("calling AutoModelForCausalLM.from_pretrained...", time.strftime("%H:%M:%S", time.localtime()))
'''
model = AutoModelForCausalLM.from_pretrained(
    model_id, trust_remote_code=True, revision=revision
)
'''
model = AutoModelForCausalLM.from_pretrained(
    model_id, trust_remote_code=True, revision=revision,
    torch_dtype=torch.float16, attn_implementation="eager"
).to("cuda")

print("calling AutoTokenizer.from_pretrained...", time.strftime("%H:%M:%S", time.localtime()))
tokenizer = AutoTokenizer.from_pretrained(model_id, revision=revision, device="cuda:0")

image = Image.open('..\\images\\MYDL2.png')
#image = Image.open('..\\images\\CSDEMOBANK.jpg')

print("calling model.encode_image...", time.strftime("%H:%M:%S", time.localtime()))
enc_image = model.encode_image(image)

print("calling model.answer_question...", time.strftime("%H:%M:%S", time.localtime()))
#print(model.answer_question(enc_image, "List all text in this image.", tokenizer))
'''
Top left corner: "LESEN MEMANDU DRIVING LICENSE"
Top right corner: "MALAYSIA"
Center: "TAKUMI TATEISHI"
Below the name: "JWAN / JAKARAN / JLKAN / CLASS / B2 D"
Below the name: "19/09/2016 - 18/04/2021"
Below the date: "42-12F CITY TOWER / JLN ALOR BKT BINTUAN / 50200 KLALU / KUALA LUMPUR"
Bottom right corner: "WILAYAH PERSEKUTUAN KLUMAPU"
'''
#print(model.answer_question(enc_image, "List all text in this image in json format.", tokenizer))
'''
- Top left corner: "LESEN MEMANDU DRIVING LICENSE"
- Top right corner: "MALAYSIA"
- Center: "TAKUMI TATEISHI"
- Below the name: "JWAN / NATIONALITY No. PENANG / IDENTITY No. TZ14051 / PIN"
- Center: "B2 D"
- Bottom left corner: "19/09/2016 - 18/04/2021"
- Bottom right corner: "42-12F CITY TOWER / JLN ALOR / BINTUAN / 50200 KLUALA LUMPUR"
- Bottom center: "WILAYAH PERSEKUTUAN KLUALA KLUALA"
'''
#print(model.answer_question(enc_image, "List all lines of text in this image.", tokenizer))
'''
Top left corner: "LESEN MEMANDU DRIVING LICENSE"
Top right corner: "MALAYSIA"
Center: "TAKUMI TATEISHI"
Below the name: "JWAN / NATIONALITY No. PENANG / IDENTITY No. TZ1450 / PIN"
Below the name: "B2 D / Class B2"
Below the class: "TEMPH / VALIDITY"
Date on the left: "19/09/2016 - 18/04/2021"
Date on the right: "19/09/2016 - 18/04/2021"
Bottom left corner: "Alamat / Address"
Bottom right corner: "50200 KLUALA LUMPUR"
Bottom right corner: "WILAYAH PERSEKUTUAN KLUALA KLUALA"
'''
print(model.answer_question(enc_image, "List all lines of text in this image in json format.", tokenizer))
'''
- "LESEN MEMANDU DRIVING LICENSE"
- "MALAYSIA"
- "TAKUMI TATEISHI"
- "JPN"
- "B2 D"
- "19.09.2016 - 18.04.2021"
- "42.12F CITY TOWER"
- "JLN ALOR BKT BINTU"
- "50200 KLUALA LUMPUR"
- "WILAYAH PERSEKUTUAN KLUALA KLUAPUR"
'''
print(model.answer_question(enc_image, "What is the name of this license holder?", tokenizer))
# The name of the license holder is Takumi Tateishi.
print(model.answer_question(enc_image, "What is the text of the field 'Warganegara / Nationality'?", tokenizer))
# JPN
print(model.answer_question(enc_image, "What is the text of the field 'No. Penganaran / Identity Number'?", tokenizer))
# TZ145051PN
#print(model.answer_question(enc_image, "What is the text of the field 'Tempoh / Validity'?", tokenizer))
# TZ1450 / PN
# print(model.answer_question(enc_image, "What is the value of the field 'Tempoh / Validity'?", tokenizer))
# The field 'Tempoh / Validity' on the driver's license has a value of 18 months.
print(model.answer_question(enc_image, "What is the text line under 'Tempoh / Validity'?", tokenizer))
# 19/09/2016 - 18/04/2021
#print(model.answer_question(enc_image, "What is the text lines of the field 'Alamat / Address'?", tokenizer))
# 19/09/2016 - 18/04/2021
#print(model.answer_question(enc_image, "List all text lines under the field 'Alamat / Address'?", tokenizer))
# 19/09/2016 - 18/04/2021
'''
image = Image.open('..\\images\\CSDEMOBANK.jpg')

print("calling model.encode_image...", time.strftime("%H:%M:%S", time.localtime()))
enc_image = model.encode_image(image)

print("calling model.answer_question...", time.strftime("%H:%M:%S", time.localtime()))
print(model.answer_question(enc_image, "List all lines of text in this image in json format.", tokenizer))
print(model.answer_question(enc_image, "What is the Name of applicant?", tokenizer))
print(model.answer_question(enc_image, "What is the text in the field 'Name (First, Sufix, Last, Middle)'?", tokenizer))
print(model.answer_question(enc_image, "What is the title of  applicant?", tokenizer))
print(model.answer_question(enc_image, "What is the velue of the check box 'Mr' in the field 'Name (First, Sufix, Last, Middle)' in this image?", tokenizer))
print(model.answer_question(enc_image, "What is the velue of the check box 'Mrs' in the field 'Name (First, Sufix, Last, Middle)' in this image?", tokenizer))
print(model.answer_question(enc_image, "What is the text in the field 'Date of Birth (mm/dd/yyyy)' in this image?", tokenizer))
print(model.answer_question(enc_image, "what is the date of birth of primary account holder in this image?", tokenizer))
print(model.answer_question(enc_image, "What is the text in the field 'Place of Birth' in this image?", tokenizer))
print(model.answer_question(enc_image, "what is the place of birth of primary account holder in this image?", tokenizer))
print(model.answer_question(enc_image, "What is the text in the field 'Primary Id' in this image?", tokenizer))
print(model.answer_question(enc_image, "what is the primary id of primary account holder in this image?", tokenizer))
print(model.answer_question(enc_image, "What is the text in the field 'Permanent Address' in this image?", tokenizer))
print(model.answer_question(enc_image, "what is the address of primary account holder in this image?", tokenizer))
'''

'''
calling model.answer_question... 07:57:50
The image contains multiple lines of text, which are not transcribed here due to the complexity and volume of the text. The text appears to be in a spreadsheet format, with various fields and fields for information. The specific content of the text is not provided, as it is not clear enough to transcribe accurately.
PEREZ
PEREZ
PEREZ BANK
0
0
08/29/1979
08/29/1979
PEREZ
The place of birth of the primary account holder in this image is in Cebu City, Philippines.
0
The primary id of the primary account holder in this image is "PEREZ BANK".
The text in the field 'Permanent Address' in this image says "PASSA 949 A. PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA PASA
PEREZ
finish:  07:59:08
'''
#print(model.answer_question(enc_image, "Describe this image.", tokenizer))
#print(model.answer_question(enc_image, "What is the text filled under 'Name (First, Sufix, Last, Middle)'?", tokenizer))
#print(model.answer_question(enc_image, "What is the place of birth?", tokenizer))
#print(model.answer_question(enc_image, "What is the permenent address?", tokenizer))
#print(model.answer_question(enc_image, "How many fields exist in this form?", tokenizer))

print("finish: ", time.strftime("%H:%M:%S", time.localtime()))
