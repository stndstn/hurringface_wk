# OCR WebAPI with Microsoft Florence-2-base

## Run as console app

    start_py_app.bat

## Run as windows service

PyAppService.exe is a windows service which start bat file, and stop it when the service stop.

### Edit start_py_app_service.bat
You can set:
- binding host name
- port

### Edit PyAppService.exe.config
You can set:
- ServiceName
- DisplayName
- StartBatFileName

### Install PyAppService.exe with InstallUtil.exe

    InstallUtil.exe PyAppService.exe

# API

## getDevice

    GET http://takumit-p:8085/device

expected response:
"cuda:0" or "cpu"

## OCR (File)

    POST http://takumit-p:8085/ocrFile

Send image file as a form data named 'file'.

## OCR (Base64)

    POST http://takumit-p:8085/ocrB64

Send image file as string value named 'b64' in json.

        { "b64": "/9j/4AAQSkZJRgABAQEBkAGQAAD/..." }

expected response:

    {
        "<OCR>": <text>
    }

## OCR with Region (File)

    POST http://takumit-p:8085/ocrWithRegionFile

Send image file as a form data named 'file'.

## OCR with Region (Base64)

    POST http://takumit-p:8085/ocrWithRegionB64

Send image file as string value named 'b64' in json.

        { "b64": "/9j/4AAQSkZJRgABAQEBkAGQAAD/..." }

expected response:

    {
        "<OCR_WITH_REGION>": {
            "labels": [
                <text block>,
                <text block>,
                <text block>
            ],
            "quad_boxes": [
                [ x_top_left, y_top_left, x_top_right, y_top_right, 
                  x_bottom_right, y_bottom_right, x_bottom_left, y_bottom_right
                ],
                [ x_top_left, y_top_left, x_top_right, y_top_right, 
                  x_bottom_right, y_bottom_right, x_bottom_left, y_bottom_right
                ],
                [ x_top_left, y_top_left, x_top_right, y_top_right, 
                  x_bottom_right, y_bottom_right, x_bottom_left, y_bottom_right
                ]
            ]
        }
    }

# Reference

## HuggingFace Microsoft Florence-2-base

https://huggingface.co/microsoft/Florence-2-base

https://huggingface.co/microsoft/Florence-2-large/blob/main/sample_inference.ipynb


## flask

https://flask.palletsprojects.com/en/3.0.x/

