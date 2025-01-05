# pip install Pillow requests torch transformers
# pip install pytesseract
from transformers import pipeline, AutoProcessor
from PIL import Image    
import requests
import torch
import pytesseract
import time

print("start: ", time.strftime("%H:%M:%S", time.localtime()))

model_id = "llava-hf/llava-1.5-7b-hf"

print("calling AutoProcessor.from_pretrained...", time.strftime("%H:%M:%S", time.localtime()))

processor = AutoProcessor.from_pretrained(model_id)
device_id = "cuda:0" if torch.cuda.is_available() else "cpu"

print("calling pipeline...", time.strftime("%H:%M:%S", time.localtime()))

# pipe = pipeline("image-to-text", model=model_id, device=device_id)
pipe = pipeline(
    "image-to-text",
    model=model_id,
    device=device_id,
)
#url = "https://huggingface.co/datasets/huggingface/documentation-images/resolve/main/transformers/tasks/ai2d-demo.jpg"
#image = Image.open(requests.get(url, stream=True).raw)

# Define a chat histiry and use `apply_chat_template` to get correctly formatted prompt
# Each value in "content" has to be a list of dicts with types ("text", "image") 
'''
conversation = [
    {
      "role": "user",
      "content": [
          {"type": "text", "text": "What does the label 15 represent? (1) lava (2) core (3) tunnel (4) ash cloud"},
          {"type": "image"},
        ],
    },
]
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
'''
[{'generated_text': "USER:  \n  List all the information in this document. \n\n
  The image shows a Malaysian driver's license with a picture of a man on it. The license is pink and white and has a sticker on the back. The man's name is Takumu Tateishi, and his birth date is listed as 1947. The license is valid until 2016."}]
[{'generated_text': 'USER:  \nThis is Mayaisian driving license. What is the name and ID number of license holder? \n\n
  The name of the license holder is Takumu Tateishi. The ID number is 441119.'}]
'''

#calling pipe... 00:35:15
#finish:  00:56:36
#[{'generated_text': 'USER:  \nList all lines of text in this image. \n\n1. A man with a beard.\n2. A man with a beard.\n3. A man with a beard.\n4. A man with a beard.\n5. A man with a beard.\n6. A man with a beard.\n7. A man with a beard.\n8. A man with a beard.\n9. A man with a beard.\n10. A man with a beard.\n11. A man with a beard.\n12. A man with a beard.\n13. A man with a beard.\n14. A man with a beard.\n15. A man with a beard.\n16. A man with a beard.\n17. A man with a beard.\n18. A man with a beard.\n19. A man with a beard.\n20. A man with a beard.\n21. A man with a beard.\n22. A man with a beard.\n23. A man with a beard.\n24. A man with a beard.\n25. A man with a beard.\n26. A man with a beard.\n27. A man with a beard.\n28. A man with a beard.\n29. A man with a beard.\n30. A man with a beard.\n31. A man with a beard.\n32. A man with a beard.\n33. A man with a beard.\n34. A man with a beard.\n35. A man with a beard.\n36. A man with a beard.\n37. A man with a beard.\n38. A man with a beard.\n39. A man with a beard.\n40. A man with a beard.\n41. A man with a beard.\n42. A man with a beard.\n43. A man with a beard.\n44. A man with a beard.\n45. A man with a beard.\n46. A man with a beard.\n4'}]

#calling pipe... 00:06:10
#finish:  00:27:27
#[{'generated_text': 'USER:  \nList all lines of text in this image in json format. \n{\n"image": {\n"filename": "takum\\_tatheishi\\_japanese\\_driving\\_license.jpg",\n"mime\\_type": "image/jpeg"\n},\n"license\\_number": "BZD D151421",\n"name": "Takum Tateishi",\n"nationality": "Japanese",\n"date\\_of\\_birth": "1974-09-19",\n"height": "170cm",\n"weight": "65kg",\n"eye\\_color": "Black",\n"hair\\_color": "Black",\n"hair\\_length": "Short",\n"hair\\_style": "Short",\n"facial\\_hair": "None",\n"skin\\_color": "Yellow",\n"complexion": "Fair",\n"ethnicity": "Asian",\n"gender": "Male",\n"occupation": "Student",\n"marital\\_status": "Single",\n"education": "High School",\n"national\\_id\\_number": "BZD D151421",\n"issuing\\_authority": "Japanese Government",\n"issuing\\_date": "2014-09-19",\n"expiry\\_date": "2016-09-19",\n"issuing\\_place": "Tokyo",\n"issuing\\_country": "Japan",\n"issuing\\_organization": "Japanese Government",\n"issuing\\_official": "Takum Tateishi",\n"issuing\\_official\\_title": "Student",\n"issuing\\_official\\_position": "Student",\n"issuing\\_official\\_department": "High School",\n"issuing\\_official\\_location": "Tokyo",\n"issuing\\_official\\_city": "Tokyo",\n"issuing\\_official\\_country": "Japan",\n"issuing\\_official\\_phone\\_number": "012-3456-7890",\n"issuing\\_official\\_email": "takum\\_tate'}]  

print("calling processor.apply_chat_template...", time.strftime("%H:%M:%S", time.localtime()))
# prompt = processor.apply_chat_template(conversation, add_generation_prompt=True)
prompt = processor.apply_chat_template(conversation, add_generation_prompt=False)

print("calling pipe...", time.strftime("%H:%M:%S", time.localtime()))
outputs = pipe(image, prompt=prompt, generate_kwargs={"max_new_tokens": 500})

print("finish: ", time.strftime("%H:%M:%S", time.localtime()))
print(outputs)
