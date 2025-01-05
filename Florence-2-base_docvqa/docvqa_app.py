# https://flask.palletsprojects.com/en/3.0.x/quickstart/
# https://code.visualstudio.com/docs/python/tutorial-flask

# https://huggingface.co/HuggingFaceM4/Florence-2-DocVQA
# https://github.com/andimarafioti/florence2-finetuning
# https://huggingface.co/blog/finetune-florence2


import sys
import time
import base64
import datetime
import os
import imghdr
import io
from PIL import Image

import ocrflorence2base
import ocrflorence2docvqa

ALLOWED_EXTENSIONS = {'png', 'jpg', 'jpeg'}


def allowed_file(filename):
    return '.' in filename and \
           filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS


def device():
    return ocrflorence2base.getDevice()


def ocrFile(filepath):
    task_prompt = "<OCR>"    
    image = Image.open(filepath)
    w = image.width
    h = image.height
    scale = 800 / max(w, h)
    if(scale < 1):
        image = image.resize((int(w * scale), int(h * scale)))

    ret = ocrflorence2base.ocr(image, task_prompt)
    if '<OCR_WITH_REGION>' in ret:
        ret["image_width"] = image.width;
        ret["image_height"] = image.height;

    return ret


def docVqaFile(filepath, text_input=None):
    task_prompt = "<DocVQA>"    
    image = Image.open(filepath)
    w = image.width
    h = image.height
    scale = 800 / max(w, h)
    if(scale < 1):
        image = image.resize((int(w * scale), int(h * scale)))

    ret = ocrflorence2docvqa.docVqa(image, text_input, task_prompt)
    if '<OCR_WITH_REGION>' in ret:
        ret["image_width"] = image.width;
        ret["image_height"] = image.height;

    return ret


# test docVQA
t_start = time.localtime()
#imageFileName = "images/MyKad1_F.jpg"
imageFileName = "images/CSDEMOBANK_ApplicationForm_P1_s.jpg"
print('start: ', t_start.tm_hour, ':', t_start.tm_min, ':', t_start.tm_sec)
#ocrFile(imageFileName) # test ocr
docVqaFile(imageFileName, "This is bank account application form. What is the name, gender, date of birth, and permanent address of this applicant?")
#docVqaFile(imageFileName, "what is this image?")
#docVqaFile(imageFileName, "This is the image of ID card of Malaysia, which is known as MyKad. What inforation can be read in this image?")
#docVqaFile(imageFileName, "what is the name of this ID card holder?")
#docVqaFile(imageFileName, "what is the ID number of this ID card holder?")
#docVqaFile(imageFileName, "Lines under ID number are address lines of ID holder. What is the Address of this ID card holder?")
#docVqaFile(imageFileName, "what is the value of 'No. Pengenalan' field in the image?")
#docVqaFile(imageFileName, "what is the value of 'Alamat' field in the image?")
t_end = time.localtime()
print('end: ', t_end.tm_hour, ':', t_end.tm_min, ':', t_end.tm_sec)
print('elapsed: ', t_end.tm_hour - t_start.tm_hour, ':', t_end.tm_min - t_start.tm_min, ':', t_end.tm_sec - t_start.tm_sec)


# Number of arguments
n = len(sys.argv)
print("Total arguments passed:", n)

# Name of Python script
# print("\nName of Python script:", sys.argv[0])

# Arguments passed
#print("\nArguments passed:", end=" ")
for i in range(1, n):
    print("\n", sys.argv[i])
    t_start = time.localtime()
    print('start: ', t_start.tm_hour, ':', t_start.tm_min, ':', t_start.tm_sec)
    #ocrFile(sys.argv[i])
    docVqaFile(sys.argv[i], "List all info of license holders in the image")
    t_end = time.localtime()
    print('end: ', t_end.tm_hour, ':', t_end.tm_min, ':', t_end.tm_sec)
    print('elapsed: ', t_end.tm_hour - t_start.tm_hour, ':', t_end.tm_min - t_start.tm_min, ':', t_end.tm_sec - t_start.tm_sec)
