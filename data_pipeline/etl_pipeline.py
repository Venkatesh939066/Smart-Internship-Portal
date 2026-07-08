import os
import json
import re
import sqlite3
import logging
from datetime import datetime, timedelta
import pandas as pd

# Setup logging
LOG_FILE = os.path.join(os.path.dirname(__file__), 'pipeline.log')
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s [%(levelname)s] %(message)s',
    handlers=[
        logging.FileHandler(LOG_FILE, mode='a', encoding='utf-8'),
        logging.StreamHandler()
    ]
)

DB_PATH = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'app.db'))
SOURCE_PATH = os.path.abspath(os.path.join(os.path.dirname(__file__), 'mock_source.json'))

def clean_html(text):
    """Remove HTML tags from raw description text."""
    if not isinstance(text, str):
        return ""
    # Strip HTML tags
    cleaned = re.sub(r'<[^<]+?>', '', text)
    # Remove excessive spaces
    cleaned = re.sub(r'\s+', ' ', cleaned).strip()
    return cleaned

def standardize_remote(text):
    """Normalize remote-work status."""
    if not isinstance(text, str):
        return "On-site"
    val = text.lower().strip()
    if 'remote' in val or 'wfh' in val or 'work from home' in val:
        return 'Remote'
    elif 'hybrid' in val:
        return 'Hybrid'
    return 'On-site'

def standardize_location(text):
    """Normalize location entries."""
    if not isinstance(text, str):
        return "Unknown"
    val = text.strip()
    # Simple Indian city normalization
    if val.lower() in ['bengaluru', 'bangalore']:
        return 'Bengaluru, India'
    elif val.lower() in ['mumbai']:
        return 'Mumbai, India'
    elif val.lower() in ['pune']:
        return 'Pune, India'
    elif val.lower() in ['hyderabad']:
        return 'Hyderabad, India'
    return val

def standardize_salary(text):
    """Normalize and format salary/stipend strings."""
    if not isinstance(text, str) or text.lower().strip() in ['none', 'null', 'unpaid', 'not disclosed']:
        return 'Not Disclosed'
    val = text.strip()
    
    # Standardize currency symbols
    if 'inr' in val.lower() or '₹' in val:
        digits = re.findall(r'\d[\d,\s]*', val)
        if digits:
            numbers = [d.replace(',', '').replace(' ', '') for d in digits]
            if len(numbers) >= 2:
                return f"₹{int(numbers[0]):,}/mo - ₹{int(numbers[1]):,}/mo"
            return f"₹{int(numbers[0]):,}/month"
        return "Stipend Provided"
        
    if 'usd' in val.lower() or '$' in val:
        digits = re.findall(r'\d[\d,\s]*', val)
        if digits:
            numbers = [d.replace(',', '').replace(' ', '') for d in digits]
            if len(numbers) >= 2:
                return f"${int(numbers[0]):,}/mo - ${int(numbers[1]):,}/mo"
            return f"${int(numbers[0]):,}/month"
        return "USD Stipend Provided"
        
    return val

def extract_skills(title, description, db_skills):
    """
    Search description and title text for skills present in the database.
    Returns a list of skill IDs.
    """
    text = f"{title} {description}".lower()
    matched_ids = []
    for skill_name, skill_id in db_skills.items():
        # Match word boundaries or special handling for HTML & CSS, C#
        pattern = re.escape(skill_name.lower())
        
        # Handle special C# match
        if skill_name.lower() == 'c#':
            pattern = r'c#'
        # Handle special ASP.NET Core
        elif skill_name.lower() == 'asp.net core':
            pattern = r'asp\.net\s*core|\.net\s*core'
        # Handle special HTML & CSS
        elif skill_name.lower() == 'html & css':
            pattern = r'html|css'
            
        if re.search(pattern, text):
            matched_ids.append(skill_id)
            
    return list(set(matched_ids))

def run_etl():
    logging.info("--- Data Ingestion & ETL Pipeline Started ---")
    
    if not os.path.exists(SOURCE_PATH):
        logging.error(f"Source file not found at: {SOURCE_PATH}")
        return
        
    if not os.path.exists(DB_PATH):
        logging.error(f"SQLite Database not found at: {DB_PATH}. Run migrations first.")
        return
        
    # Connect to SQLite
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        logging.info(f"Connected to SQLite Database: {DB_PATH}")
    except Exception as e:
        logging.error(f"Failed to connect to database: {e}")
        return

    # Extract raw data
    try:
        raw_df = pd.read_json(SOURCE_PATH)
        logging.info(f"Extracted {len(raw_df)} raw listings from mock_source.json")
    except Exception as e:
        logging.error(f"Failed to load raw JSON: {e}")
        conn.close()
        return

    # Fetch Companies (ApplicationUser with Role='Company')
    cursor.execute("SELECT Id, FullName FROM AspNetUsers WHERE Role = 'Company'")
    db_companies = {row[1].lower().strip(): row[0] for row in cursor.fetchall()}
    logging.info(f"Found {len(db_companies)} seeded companies in database.")

    # Fetch existing Skills
    cursor.execute("SELECT Id, Name FROM Skills")
    db_skills = {row[1]: row[0] for row in cursor.fetchall()}
    logging.info(f"Found {len(db_skills)} existing skills in database.")

    # Transformations using Pandas
    raw_df['CleanDescription'] = raw_df['Description'].apply(clean_html)
    raw_df['CleanLocation'] = raw_df['Location'].apply(standardize_location)
    raw_df['CleanRemoteType'] = raw_df['RemoteType'].apply(standardize_remote)
    raw_df['CleanSalary'] = raw_df['Salary'].apply(standardize_salary)
    
    # Initialize metrics
    inserted_count = 0
    updated_count = 0
    error_count = 0

    for idx, row in raw_df.iterrows():
        try:
            title = row['Title']
            description = row['CleanDescription']
            location = row['CleanLocation']
            salary = row['CleanSalary']
            job_type = row['JobType']
            remote_type = row['CleanRemoteType']
            exp_level = row.get('ExperienceLevel', 'Entry Level')
            apply_url = row.get('ApplyUrl', 'https://example.com/apply')
            
            # 1. Parse dates
            posted_date_str = str(row['PostedDate'])
            try:
                posted_date = pd.to_datetime(posted_date_str).strftime('%Y-%m-%d %H:%M:%S')
                deadline = (pd.to_datetime(posted_date_str) + timedelta(days=30)).strftime('%Y-%m-%d %H:%M:%S')
            except Exception:
                posted_date = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
                deadline = (datetime.now() + timedelta(days=30)).strftime('%Y-%m-%d %H:%M:%S')

            # 2. Map Company ID
            company_name = row['CompanyName'].lower().strip()
            # Try exact match, otherwise partial match, otherwise fall back to first available company
            company_id = None
            for key, val in db_companies.items():
                if company_name in key or key in company_name:
                    company_id = val
                    break
            
            if not company_id:
                if db_companies:
                    company_id = list(db_companies.values())[0] # fallback
                    logging.warning(f"Company '{row['CompanyName']}' not found in database. Mapped to default company ID.")
                else:
                    logging.error(f"No company accounts found in database. Skipping row {idx}.")
                    error_count += 1
                    continue
            
            # Determine company logo fallback
            company_logo = f"/images/logos/{row['CompanyName'].lower().replace(' ', '_')}.png"
            if not os.path.exists(os.path.join(os.path.dirname(__file__), '..', 'wwwroot', 'images', 'logos')):
                company_logo = "/images/company-placeholder.svg"

            # 3. Check for existence (Upsert)
            cursor.execute(
                "SELECT Id FROM Internships WHERE Title = ? AND CompanyId = ?", 
                (title, company_id)
            )
            existing = cursor.fetchone()
            
            if existing:
                internship_id = existing[0]
                cursor.execute("""
                    UPDATE Internships 
                    SET Description = ?, Location = ?, Salary = ?, JobType = ?, RemoteType = ?, 
                        ExperienceLevel = ?, PostedDate = ?, Deadline = ?, CompanyLogo = ?, ApplyUrl = ?
                    WHERE Id = ?
                """, (description, location, salary, job_type, remote_type, exp_level, posted_date, deadline, company_logo, apply_url, internship_id))
                updated_count += 1
                logging.info(f"Updated internship: '{title}' by company ID {company_id}")
            else:
                cursor.execute("""
                    INSERT INTO Internships (Title, Description, CompanyId, Location, Salary, JobType, RemoteType, ExperienceLevel, PostedDate, Deadline, CompanyLogo, ApplyUrl)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (title, description, company_id, location, salary, job_type, remote_type, exp_level, posted_date, deadline, company_logo, apply_url))
                internship_id = cursor.lastrowid
                inserted_count += 1
                logging.info(f"Inserted new internship: '{title}' (ID: {internship_id})")

            # 4. Map skills to this internship
            matched_skill_ids = extract_skills(title, description, db_skills)
            
            # Clear previous skills
            cursor.execute("DELETE FROM InternshipSkills WHERE InternshipId = ?", (internship_id,))
            
            # Load new skill relationships
            for skill_id in matched_skill_ids:
                cursor.execute(
                    "INSERT INTO InternshipSkills (InternshipId, SkillId) VALUES (?, ?)", 
                    (internship_id, skill_id)
                )
            
        except Exception as e:
            logging.error(f"Error processing row {idx}: {e}")
            error_count += 1
            continue

    # Commit transactions
    conn.commit()
    conn.close()
    
    # Save Pipeline run summary log
    logging.info("--- Ingestion Pipeline Finished ---")
    logging.info(f"Summary: Loaded {inserted_count} new postings, Updated {updated_count} postings, Errors: {error_count}")
    print(f"\nSUCCESS: Pipeline finished successfully.\nInserted: {inserted_count}\nUpdated: {updated_count}\nErrors: {error_count}\nLog written to: {LOG_FILE}")

if __name__ == '__main__':
    run_etl()
