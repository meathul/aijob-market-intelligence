import urllib.request
import json
from collections import Counter

url = "http://localhost:5062/api/jobs"
try:
    with urllib.request.urlopen(url) as response:
        data = json.loads(response.read().decode('utf-8'))
        jobs = data.get("jobs", [])
        print(f"Total jobs returned: {len(jobs)}")
        
        urls = [j.get("url") for j in jobs]
        titles = [j.get("title") for j in jobs]
        ids = [j.get("id") for j in jobs]
        
        print("\nURL Counts:")
        url_counts = Counter(urls)
        for u, count in url_counts.most_common(5):
            print(f"  {u}: {count}")
            
        print("\nTitle Counts:")
        title_counts = Counter(titles)
        for t, count in title_counts.most_common(5):
            print(f"  {t}: {count}")
            
        print("\nID Counts:")
        id_counts = Counter(ids)
        for i, count in id_counts.most_common(5):
            print(f"  {i}: {count}")
            
except Exception as e:
    print(f"Error: {e}")
