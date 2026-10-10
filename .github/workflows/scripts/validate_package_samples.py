#!/usr/bin/env python3
import json
import sys
from pathlib import Path


# contents = ""
filePath = './package.json'

def validate_package_samples():



    # 1. Get file
    contents = get_file(filePath)
    # try:
    #     file = open(filePath) 
    #     contents = file.read()

    # except:
    #     print("File could not find")
    #     sys.exit(1)

    # 2. Turn it into JSON
    jFile = convert_to_json(contents)
    # jFile = json.loads(contents)
    # print("\"package.json\" found.")

    # 3. Validate Display Names of Sample
    validate_display_names(jFile)
    # for sample in jFile["samples"]:
    #     moduleName = sample["displayName"]
    #     if(not str.endswith(moduleName, 'Module')):
    #         print(f"[package.json]: \"{moduleName}\" is not following rules -> XXXXXXXX Module")
    #         sys.exit(1)

    # print("All sample display names are validated.")

    # 4. Validate Paths
    validate_sample_paths(jFile)
    # for sample in jFile["samples"]:
    #     samplePath = sample["path"]
    #     if not Path(samplePath).is_dir():
    #         print(f"[package.json]: \"{samplePath}\" is defined but does not exist.")
    #         sys.exit(1)

    # print("All sample paths are validated.")

            

def get_file(filePath):
    try:
        file = open(filePath) 
        contents = file.read()
    
    except:
        print("File could not find")
        sys.exit(1)

    return contents


def convert_to_json(contents):
    jFile = json.loads(contents)
    print("\"package.json\" converted.")
    
    return jFile

def validate_display_names(jFile):
    for sample in jFile["samples"]:
        moduleName = sample["displayName"]
        if(not str.endswith(moduleName, 'Module')):
            print(f"[package.json]: \"{moduleName}\" is not following rules -> XXXXXXXX Module")
            sys.exit(1)
    
    print("All sample display names are validated.")
    

def validate_sample_paths(jFile):
    for sample in jFile["samples"]:
        samplePath = sample["path"]
        if not Path(samplePath).is_dir():
            print(f"[package.json]: \"{samplePath}\" is defined but does not exist.")
            sys.exit(1)
    print("All sample paths are validated.")
    





if __name__ == "__main__":
    validate_package_samples()