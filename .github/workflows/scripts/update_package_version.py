import os, re, json

from utils import get_file, convert_to_json, filePath

def update_package_version():
    
    # 1. Get file
    contents = get_file(filePath, 'r+')
    
    # 2. Turn it into JSON
    jFile = convert_to_json(contents)
    
    bump_type = os.getenv("BUMP_TYPE", "patch")
    
    version = jFile["version"]
    
    match = re.match(r'^(\d+)\.(\d+)\.(\d+)$', version)
    
    if match:
        major, minor, patch = map(int, match.groups())
        
        print(f"Current Major Version: {major}")
        print(f"Current Minor Version: {minor}")
        print(f"Current Patch Version: {patch}")
        
        if bump_type == "Major":
            major += 1
            minor = 0
            patch = 0
        elif bump_type == "Minor":
            minor += 1
            patch = 0
        else:
            patch += 1
            
        new_version = f"{major}.{minor}.{patch}"
    else:
        new_version = "0.0.1"
        
    if "version" in jFile:
        jFile['version'] = new_version
        
    modifiedjFile = json.dumps(jFile, indent=2)
    
    print(modifiedjFile)
    
    with open(filePath, "w") as packageFile:
        packageFile.write(modifiedjFile)
        
    
    
    
if __name__ == "__main__":
    update_package_version()