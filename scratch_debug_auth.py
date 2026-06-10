import urllib.request
import json

# 1. Log in
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
        print("Login response:")
        print(json.dumps(res_data, indent=2))
        
        token = res_data.get("token")
        
        # 2. Call recommendations endpoint
        rec_url = "http://localhost:5062/api/JobsRecommendations?take=20"
        rec_req = urllib.request.Request(
            rec_url,
            headers={
                'Authorization': f'Bearer {token}',
                'Content-Type': 'application/json'
            }
        )
        
        try:
            with urllib.request.urlopen(rec_req) as rec_response:
                rec_res_data = json.loads(rec_response.read().decode('utf-8'))
                print("Recommendations response:")
                print(json.dumps(rec_res_data, indent=2))
        except urllib.error.HTTPError as he:
            print(f"HTTPError on Recommendations API: {he.code} {he.reason}")
            print(he.read().decode('utf-8'))
            
except Exception as e:
    print(f"Error: {e}")
