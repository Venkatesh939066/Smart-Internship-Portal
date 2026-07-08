import os
import sys
import subprocess
import webbrowser
import time

def main():
    print("====================================================")
    print("   Smart Internship Portal - Data Platform Booter   ")
    print("====================================================")
    
    # Get workspace root path
    root_dir = os.path.dirname(os.path.abspath(__file__))
    os.chdir(root_dir)

    # 1. Install/Verify Python Libraries
    print("\n[Step 1/4] Checking and installing Python libraries...")
    req_path = os.path.join(root_dir, "data_pipeline", "requirements.txt")
    try:
        subprocess.check_call([sys.executable, "-m", "pip", "install", "-r", req_path])
        print("SUCCESS: Python libraries are fully installed and verified.")
    except Exception as e:
        print(f"WARNING: Failed to run pip install. Make sure python is added to your PATH. Error: {e}")

    # 2. Run the ETL Pipeline Ingestion
    print("\n[Step 2/4] Running Ingestion & ETL pipeline...")
    etl_path = os.path.join(root_dir, "data_pipeline", "etl_pipeline.py")
    try:
        subprocess.check_call([sys.executable, etl_path])
        print("SUCCESS: ETL pipeline executed. Database has been updated with raw mock source listings.")
    except Exception as e:
        print(f"ERROR: ETL pipeline execution failed: {e}")
        return

    # 3. Spin up the Streamlit BI Dashboard
    print("\n[Step-3/4] Starting Streamlit BI Dashboard...")
    dash_path = os.path.join(root_dir, "data_pipeline", "dashboard.py")
    streamlit_proc = None
    try:
        # Launch streamlit in a separate process
        streamlit_proc = subprocess.Popen([
            sys.executable, "-m", "streamlit", "run", dash_path,
            "--server.port", "8501"
        ])
        print("Streamlit dashboard process launched.")
    except Exception as e:
        print(f"ERROR: Failed to launch Streamlit dashboard: {e}")

    # 4. Spin up the ASP.NET Core Web Portal
    print("\n[Step 4/4] Starting ASP.NET Core Web Portal...")
    dotnet_proc = None
    try:
        # Launch dotnet run in a separate process
        dotnet_proc = subprocess.Popen(["dotnet", "run"], shell=True)
        print("ASP.NET Core web server process launched.")
    except Exception as e:
        print(f"ERROR: Failed to launch ASP.NET Core portal. Make sure dotnet SDK is installed. Error: {e}")

    print("\n====================================================")
    print("Platform is now booting up!")
    print("- Web Portal: http://localhost:5082")
    print("- Streamlit Dashboard: http://localhost:8501")
    print("Press Ctrl+C in this terminal to shut down both servers.")
    print("====================================================")

    # Give it a few seconds to start before opening browser pages
    time.sleep(5)
    
    # Open local endpoints in the default browser
    try:
        webbrowser.open("http://localhost:5082/Home/Analytics")
        webbrowser.open("http://localhost:8501")
    except Exception:
        pass

    try:
        # Keep runner alive to monitor processes
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\nShutting down servers...")
        if streamlit_proc:
            streamlit_proc.terminate()
            print("Streamlit server stopped.")
        if dotnet_proc:
            # Under Windows, killing shell processes requires taskkill
            subprocess.call("taskkill /f /im new.exe", shell=True)
            print("ASP.NET web server stopped.")
        print("Data Platform Booter shut down successfully.")

if __name__ == "__main__":
    main()
