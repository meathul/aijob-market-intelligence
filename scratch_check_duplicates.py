import urllib.request
import json
from collections import Counter

url = "http://localhost:5062/api/jobs"
try:
    with urllib.request.urlopen(url) as response:
        data = json.loads(response.read().decode('utf-8'))
        jobs = data.get("jobs", [])
        
        # Simulate mapping logic
        mapped_jobs = []
        for r in jobs:
            mapped_job = {
                "title": r.get("title") or "—",
                "url": r.get("url") or None
            }
            mapped_jobs.append(mapped_job)
            
        # Simulate trackby key: row.url ?? row.title
        track_keys = []
        for row in mapped_jobs:
            key = row["url"] if row["url"] is not None else row["title"]
            track_keys.append(key)
            
        print(f"Total mapped jobs: {len(mapped_jobs)}")
        print(f"Total unique track keys: {len(set(track_keys))}")
        
        if len(track_keys) != len(set(track_keys)):
            print("\nDUPLICATES FOUND!")
            counts = Counter(track_keys)
            for k, count in counts.most_common():
                if count > 1:
                    print(f"  Key: '{k}' appears {count} times")
        else:
            print("\nNo duplicates found in track keys!")
            
except Exception as e:
    print(f"Error: {e}")
