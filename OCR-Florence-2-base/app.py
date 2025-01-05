# https://flask.palletsprojects.com/en/3.0.x/quickstart/
# https://code.visualstudio.com/docs/python/tutorial-flask

import base64
import datetime
import os
import imghdr
import io
from PIL import Image
from flask import Flask, request, jsonify
from werkzeug.utils import secure_filename
from werkzeug.exceptions import HTTPException
# pip freeze > requirements.txt
# pip install -r requirements.txt
import ocrflorence2base

#UPLOAD_FOLDER = '/path/to/the/uploads'
UPLOAD_FOLDER = 'c:\\temp\\flask_test\\uploads'
ALLOWED_EXTENSIONS = {'txt', 'pdf', 'png', 'jpg', 'jpeg', 'gif'}

app = Flask(__name__)
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER


class UnknownError(Exception):
    status_code = 400

    def __init__(self, message, status_code=None, payload=None):
        super().__init__()
        self.message = message
        if status_code is not None:
            self.status_code = status_code
        self.payload = payload

    def to_dict(self):
        rv = dict(self.payload or ())
        rv['message'] = self.message
        return rv

@app.errorhandler(UnknownError)
def invalid_api_usage(e):
    return jsonify(e.to_dict()), e.status_code


@app.route("/")
def hello_world():
    return "<p>Hello, World!</p>"


@app.route('/device')
def device():
    return ocrflorence2base.getDevice()


@app.route('/ocr', methods=['POST'])
def ocr():
    if request.method == 'POST':        
        if request.is_json:
            #print(request.json)
            b64 = request.json['b64']
            if b64 != None:
                # convert bsae64 string to bytestream
                #bstream = base64.b64decode(b64)
                # convert base64 string to bytearray and save to disk
                img_data = base64.b64decode(b64)
                whatfile = imghdr.what(None, img_data)
                print(whatfile)
                # set filename as yyyymmddhhmmssfff formatted date now
                now = datetime.datetime.now()
                filename = now.strftime("%Y%m%d%H%M%S%f") + '.' + whatfile
                filename = secure_filename(filename)
                filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
                with open(filepath, 'wb') as f:
                    f.write(img_data)
                image = Image.open(io.BytesIO(img_data))
                return ocrflorence2base.ocr(image)

        # check if the post request has the file part
        if 'file' not in request.files:
            raise UnknownError("'file' not in request.files")
        
        file = request.files['file']
        # If the user does not select a file, the browser submits an
        # empty file without a filename.        
        if file == None or file.filename == '':
            raise UnknownError('No selected file')
        
        if file and allowed_file(file.filename):
            filename = secure_filename(file.filename)
            filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
            file.save(filepath)
            image = Image.open(filepath)
            return ocrflorence2base.ocr(image)
        
    return 


@app.route('/ocrwithregion', methods=['POST'])
@app.route('/ocrWithRegion', methods=['POST'])
def ocrWithRegion():
    if request.method == 'POST':        
        if request.is_json:
            #print(request.json)
            b64 = request.json['b64']
            if b64 != None:
                # convert bsae64 string to bytestream
                #bstream = base64.b64decode(b64)
                # convert base64 string to bytearray and save to disk
                img_data = base64.b64decode(b64)
                whatfile = imghdr.what(None, img_data)
                print(whatfile)
                # set filename as yyyymmddhhmmssfff formatted date now
                now = datetime.datetime.now()
                filename = now.strftime("%Y%m%d%H%M%S%f") + '.' + whatfile
                filename = secure_filename(filename)
                filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
                with open(filepath, 'wb') as f:
                    f.write(img_data)
                image = Image.open(io.BytesIO(img_data))
                return ocrflorence2base.ocrWithRegion(image)

        # check if the post request has the file part
        if 'file' not in request.files:
            raise UnknownError("'file' not in request.files")
        
        file = request.files['file']
        # If the user does not select a file, the browser submits an
        # empty file without a filename.        
        if file == None or file.filename == '':
            raise UnknownError('No selected file')
        
        if file and allowed_file(file.filename):
            filename = secure_filename(file.filename)
            filepath = os.path.join(app.config['UPLOAD_FOLDER'], filename)
            file.save(filepath)
            image = Image.open(filepath)
            return ocrflorence2base.ocrWithRegion(image)
        
    return 
