import urllib.request
import json
from collections import Counter

# 1. Log in to get token
login_url = "http://localhost:5062/api/Auth/login"
login_data = json.dumps({
    "email": "admin@local",
    "password": "Admin@12345"
}).encode('utf-8')

req = urllib.request.Request(
    login_url,
    data=login_data,
    headers={'Content-Type': 'application/json'}
)

try:
    with urllib.request.urlopen(req) as response:
        res_data = json.loads(response.read().decode('utf-8'))
        token = res_data.get("accessToken")
        print(f"Successfully logged in and obtained token: {token[:20]}...")
        
        # 2. Get recommendations
        rec_url = "http://localhost:5062/api/JobsRecommendations?take=20"
        rec_req = urllib.request.Request(
            rec_url,
            headers={
                'Authorization': f'Bearer {token}',
                'Content-Type': 'application/json'
            }
        )
        
        with urllib.request.urlopen(rec_req) as rec_response:
            rec_data = json.loads(rec_response.read().decode('utf-8'))
            jobs = rec_data.get("jobs", [])
            print(f"Total recommended jobs returned: {len(jobs)}")
            
            # Map recommendations
            mapped_jobs = []
            for item in jobs:
                j = item.get("job", {})
                mapped_job = {
                    "title": j.get("title") or "—",
                    "url": j.get("url") or None
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
                print("\nDUPLICATES FOUND IN RECOMMENDATIONS!")
                counts = Counter(track_keys)
                for k, count in counts.most_common():
                    if count > 1:
                        print(f"  Key: '{k}' appears {count} times")
            else:
                print("\nNo duplicates found in recommendations track keys!")
                
except Exception as e:
    print(f"Error: {e}")
