# https://huggingface.co/impira/layoutlm-document-qa
# To run these examples, you must have PIL(pillow), pytesseract, and PyTorch installed in addition to transformers.
# pip install pillow pytesseract
# pip3 install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
# pip install transformers

from transformers import pipeline
import pytesseract
import PIL.Image as Image
import time

print("start: ", time.strftime("%H:%M:%S", time.localtime()))

# https://stackoverflow.com/questions/50655738/how-do-i-resolve-a-tesseractnotfounderror
# https://pypi.org/project/pytesseract/
pytesseract.pytesseract.tesseract_cmd = "C:\\Program Files\\Tesseract-OCR\\tesseract.exe"

print("calling pipeline...", time.strftime("%H:%M:%S", time.localtime()))
nlp = pipeline(
    "document-question-answering",
    model="impira/layoutlm-document-qa",
    device="cuda",
)

#outputs = nlp(
#    "https://templates.invoicehome.com/invoice-template-us-neat-750px.png",
#    "What is the invoice number?"
#)
# {'score': 0.9943977, 'answer': 'us-001', 'start': 15, 'end': 15}


image = Image.open("..\\..\\images\\CSDEMOBANK.jpg")
print("calling nlp...", time.strftime("%H:%M:%S", time.localtime()))
outputs = nlp(
    [
        {"image": image, "question": "What is the title of this form?"},
        {"image": image, "question": "What is the name of applicant in 'SECTION A PERSONAL INFORMATION'?"},
        {"image": image, "question": "What is gender of applicant?"},
        {"image": image, "question": "Which title is selected as applicant's title?"}, 
        {"image": image, "question": "Which title is selected as applicant's title from 'Mr.', 'Mrs.', 'Ms.', or 'Others'?"}, 
        {"image": image, "question": "When is date of birth of applicant?"}, 
        {"image": image, "question": "What is the text filled in the box 'Place of Birth' in 'SECTION A PERSONAL INFORMATION'?"}, 
    ]
)
print(outputs)
'''
calling nlp... 07:30:55
[
    [{'score': 0.26583611965179443, 'answer': 'INDIVIDUAL APPLICATION FORM FOR DEPOSIT ACCOUNT', 'start': 2, 'end': 7}], 
    [{'score': 0.999924898147583, 'answer': 'PERE? FELIX', 'start': 28, 'end': 29}], 
    [{'score': 0.35627350211143494, 'answer': 'Married', 'start': 47, 'end': 47}], 
    [{'score': 0.4158439338207245, 'answer': 'PERMANENT', 'start': 109, 'end': 109}], 
    [{'score': 0.05603988468647003, 'answer': 'PERE? FELIX', 'start': 28, 'end': 29}], 
    [{'score': 0.005543902050703764, 'answer': 'BULACAN', 'start': 40, 'end': 40}], 
    [{'score': 0.9995900988578796, 'answer': 'BULACAN', 'start': 40, 'end': 40}]
]
'''
image2 = Image.open("..\\..\\images\\MYDL2.png")
print("calling nlp...", time.strftime("%H:%M:%S", time.localtime()))
outputs = nlp(
    [
        {"image": image2, "question": "What is the name of this driving license holder?"},
        {"image": image2, "question": "What is 'Warganegara / Nationality'?"},
        {"image": image2, "question": "What is 'No. Penganaran / Identity Number'?"},
        {"image": image2, "question": "What is 'Tempoh / Validity'?"},
        {"image": image2, "question": "What is the line under 'Tempoh / Validity'?"},
        {"image": image2, "question": "What is 'Alamat / Address'?"},
        {"image": image2, "question": "What is the 1st line of text'?"},
        {"image": image2, "question": "What is the 2nd line of text'?"},
        {"image": image2, "question": "What is the last line of text'?"},
    ]
)
print(outputs)
'''
calling nlp... 07:31:32
[
    [{'score': 0.9999547004699707, 'answer': 'TAKUMI TATEISHI', 'start': 7, 'end': 8}], 
    [{'score': 0.8405294418334961, 'answer': 'JPN', 'start': 14, 'end': 14}], 
    [{'score': 0.9275992512702942, 'answer': 'TZ1145051JPN', 'start': 15, 'end': 15}], 
    [{'score': 0.9862931966781616, 'answer': '19/09/2016', 'start': 26, 'end': 26}], 
    [{'score': 0.9948939681053162, 'answer': '19/09/2016', 'start': 26, 'end': 26}], 
    [{'score': 0.7095370292663574, 'answer': '42-12F CITY TOWER', 'start': 31, 'end': 33}], 
    [{'score': 0.0827254205942154, 'answer': 'MALAYSIA DRIVING', 'start': 3, 'end': 4}], 
    [{'score': 0.44317564368247986, 'answer': 'B2D', 'start': 19, 'end': 19}], 
    [{'score': 0.8025591969490051, 'answer': 'Validity', 'start': 25, 'end': 25}]
]
finished:  07:32:16
'''

#outputs = nlp(
#    "https://miro.medium.com/max/787/1*iECQRIiOGTmEFLdWkVIH2g.jpeg",
#    "What is the purchase amount?"
#)
# {'score': 0.9912159, 'answer': '$1,000,000,000', 'start': 97, 'end': 97}
#print(outputs)

#outputs = nlp(
#    "https://www.accountingcoach.com/wp-content/uploads/2013/10/income-statement-example@2x.png",
#    "What are the 2020 net sales?"
#)
# {'score': 0.59147286, 'answer': '$ 3,750', 'start': 19, 'end': 20}
#print(outputs)

print("finished: ", time.strftime("%H:%M:%S", time.localtime()))

