#!/usr/bin/env python3
import json
import sys
from pathlib import Path


contents = ""
filePath = './package.json'

def validate_package_samples():



    # 1. Get file
    # file = get_file()
    try:
        file = open(filePath) 
        contents = file.read()

    except:
        print("File could not find")
        sys.exit(1)

    # 2. Turn it into JSON
    # convert_to_json(file)
    jFile = json.loads(contents)
    print("\"package.json\" found.")

    # 3. Validate Display Names of Sample
    for sample in jFile["samples"]:
        moduleName = sample["displayName"]
        if(not str.endswith(moduleName, 'Module')):
            print(f"[package.json]: \"{moduleName}\" is not following rules -> XXXXXXXX Module")
            sys.exit(1)

    print("All sample display names are validated.")

    # 4. Validate Paths
    for sample in jFile["samples"]:
            samplePath = sample["path"]
            # print(samplePath)
            if(not Path.is_dir(samplePath)):
                print(f"[package.json]: \"{samplePath}\" is defined but does not exist.")
                sys.exit(1)

    print("All sample paths are validated.")

            


    







if __name__ == "__main__":
    validate_package_samples()