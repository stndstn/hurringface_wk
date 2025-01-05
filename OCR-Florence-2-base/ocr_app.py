# https://flask.palletsprojects.com/en/3.0.x/quickstart/
# https://code.visualstudio.com/docs/python/tutorial-flask

import sys
import time
import base64
import datetime
import os
import imghdr
import io
from PIL import Image

import ocrflorence2base

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
    ocrFile(sys.argv[i])
    t_end = time.localtime()
    print('end: ', t_end.tm_hour, ':', t_end.tm_min, ':', t_end.tm_sec)
    print('elapsed: ', t_end.tm_hour - t_start.tm_hour, ':', t_end.tm_min - t_start.tm_min, ':', t_end.tm_sec - t_start.tm_sec)
