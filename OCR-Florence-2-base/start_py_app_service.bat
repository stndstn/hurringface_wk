rem call ..\WPy64-3830\scripts\env.bat
call .\.venv\Scripts\activate.bat

rem python.exe -m flask run
python.exe -m flask run --host=0.0.0.0 -p 8085 1> log.txt 2>&1
