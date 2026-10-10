import json
import sys
from pathlib import Path

filePath = "./package.json"

def get_file(filePath):
    """ Gets file from path. Read-Only """
    
    contents = get_file(filePath, 'r')
    return contents


def get_file(filePath, format):
    """ Gets file from path with type. """
    
    try:
        file = open(filePath, format) 
        contents = file.read()
    
    except:
        print("File could not find")
        sys.exit(1)

    return contents



def convert_to_json(contents):
    """ Converts string contents into json format. """
    
    jFile = json.loads(contents)
    print("\"package.json\" converted.")
    
    return jFile