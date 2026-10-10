import os
import re
import json

def increment_version():
    filePath = './package.json'
    
    with open(filePath, 'r', encoding='utf-8') as file:
        data = json.load(file)

    current_version = data.get("version", "1.0.0")
    
    bump_type = os.getenv("BUMP_TYPE", "patch")

    match = re.match(r'^(\d+)\.(\d+)\.(\d+)$', current_version)
    if match:
        major, minor, patch = map(int, match.groups())
        
        if bump_type == "major":
            major += 1
            minor = 0
            patch = 0
        elif bump_type == "minor":
            minor += 1
            patch = 0
        else:
            patch += 1
            
        new_version = f"{major}.{minor}.{patch}"
    else:
        new_version = "0.0.1"

    data["version"] = new_version

    with open(filePath, 'w', encoding='utf-8') as file:
        json.dump(data, file, indent=2)
        file.write('\n')

    print(f"Version bumped ({bump_type}): {current_version} -> {new_version}")