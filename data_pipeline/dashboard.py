import os
import re
import sqlite3
import pandas as pd
import streamlit as st
import plotly.express as px
import plotly.graph_objects as go

# Streamlit Page Config
st.set_page_config(
    page_title="Internship Market Insights",
    page_icon="📊",
    layout="wide",
    initial_sidebar_state="expanded"
)

# Custom Styling for Premium Aesthetics
st.markdown("""
<style>
    .main {
        background-color: #0e1117;
        color: #ffffff;
    }
    .metric-card {
        background-color: #1f2937;
        border: 1px solid #374151;
        padding: 20px;
        border-radius: 10px;
        text-align: center;
        box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
    }
    .metric-value {
        font-size: 2.2rem;
        font-weight: 700;
        color: #60a5fa;
        margin-bottom: 5px;
    }
    .metric-label {
        font-size: 0.9rem;
        color: #9ca3af;
        text-transform: uppercase;
        letter-spacing: 0.05em;
    }
    h1, h2, h3 {
        font-family: 'Outfit', 'Inter', sans-serif;
    }
</style>
""", unsafe_allow_html=True)

# Database path resolution
DB_PATH = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'app.db'))

def load_data():
    if not os.path.exists(DB_PATH):
        st.error(f"SQLite Database app.db not found at: {DB_PATH}. Please run the ETL pipeline first.")
        return None, None, None

    try:
        conn = sqlite3.connect(DB_PATH)
        # Fetch internships with Company Name
        query_jobs = """
            SELECT 
                i.Id as InternshipId, i.Title, i.Description, i.Location, 
                i.Salary, i.JobType, i.RemoteType, i.ExperienceLevel, 
                i.PostedDate, u.FullName as CompanyName
            FROM Internships i
            LEFT JOIN AspNetUsers u ON i.CompanyId = u.Id
        """
        df_jobs = pd.read_sql_query(query_jobs, conn)
        
        # Fetch Internship Skills mapping
        query_skills = """
            SELECT 
                isk.InternshipId, s.Name as SkillName
            FROM InternshipSkills isk
            INNER JOIN Skills s ON isk.SkillId = s.Id
        """
        df_skills = pd.read_sql_query(query_skills, conn)
        
        # Fetch all skills
        df_all_skills = pd.read_sql_query("SELECT * FROM Skills", conn)
        
        conn.close()
        return df_jobs, df_skills, df_all_skills
    except Exception as e:
        st.error(f"Error loading database content: {e}")
        return None, None, None

# Load Datasets
df_jobs, df_skills, df_all_skills = load_data()

if df_jobs is not None:
    # --- Sidebar Filtering Controls ---
    st.sidebar.image("https://cdn-icons-png.flaticon.com/512/2103/2103633.png", width=70)
    st.sidebar.title("Data Pipeline Filters")
    st.sidebar.markdown("Sift through real-time processed listings loaded from the ETL pipeline.")

    # Search Filter
    search_query = st.sidebar.text_input("🔍 Search Job Title / Keywords", "")

    # Company Filter
    all_companies = ["All"] + list(df_jobs["CompanyName"].dropna().unique())
    selected_company = st.sidebar.selectbox("🏢 Select Recruiter Company", all_companies)

    # Remote Mode Filter
    all_remote_modes = ["All"] + list(df_jobs["RemoteType"].dropna().unique())
    selected_remote = st.sidebar.selectbox("💻 Work Mode Preference", all_remote_modes)

    # Skills Multi-select Filter
    all_skill_list = list(df_all_skills["Name"].unique())
    selected_skills = st.sidebar.multiselect("🛠️ Filter by Technical Skills", all_skill_list)

    # Filter operations
    filtered_df = df_jobs.copy()

    if search_query:
        filtered_df = filtered_df[
            filtered_df["Title"].str.contains(search_query, case=False, na=False) |
            filtered_df["Description"].str.contains(search_query, case=False, na=False)
        ]

    if selected_company != "All":
        filtered_df = filtered_df[filtered_df["CompanyName"] == selected_company]

    if selected_remote != "All":
        filtered_df = filtered_df[filtered_df["RemoteType"] == selected_remote]

    # Skill intersection filtering
    if selected_skills:
        # Find internship IDs that match all selected skills
        matching_job_ids = []
        for job_id in filtered_df["InternshipId"].unique():
            job_skills = set(df_skills[df_skills["InternshipId"] == job_id]["SkillName"].values)
            if set(selected_skills).issubset(job_skills):
                matching_job_ids.append(job_id)
        filtered_df = filtered_df[filtered_df["InternshipId"].isin(matching_job_ids)]

    # --- Dashboard Page Header ---
    st.title("📊 Internship Market Intelligence Hub")
    st.markdown("An interactive Business Intelligence dashboard powered by **Python**, **Pandas**, and **Streamlit**, reading live metrics from the portal database.")

    # --- KPI Grid Cards ---
    col1, col2, col3, col4 = st.columns(4)

    # Total Jobs
    with col1:
        st.markdown(f"""
        <div class="metric-card">
            <div class="metric-value">{len(filtered_df)}</div>
            <div class="metric-label">Processed Listings</div>
        </div>
        """, unsafe_allow_html=True)

    # Top Demanded Skill
    with col2:
        if not filtered_df.empty:
            merged_skills = df_skills[df_skills["InternshipId"].isin(filtered_df["InternshipId"])]
            top_skill = merged_skills["SkillName"].value_counts().idxmax() if not merged_skills.empty else "N/A"
            top_skill_count = merged_skills["SkillName"].value_counts().max() if not merged_skills.empty else 0
            skill_display = f"{top_skill} ({top_skill_count})" if top_skill_count > 0 else "N/A"
        else:
            skill_display = "N/A"
            
        st.markdown(f"""
        <div class="metric-card">
            <div class="metric-value">{skill_display}</div>
            <div class="metric-label">Top Demanded Skill</div>
        </div>
        """, unsafe_allow_html=True)

    # Unique Recruiters
    with col3:
        recruiters_count = filtered_df["CompanyName"].nunique()
        st.markdown(f"""
        <div class="metric-card">
            <div class="metric-value">{recruiters_count}</div>
            <div class="metric-label">Active Recruiters</div>
        </div>
        """, unsafe_allow_html=True)

    # Average Estimated Stipend (INR Filter)
    with col4:
        # Approximate average from string parsing
        salaries = filtered_df["Salary"].dropna()
        rupee_salaries = []
        for s in salaries:
            if '₹' in s or 'inr' in s.lower():
                nums = [int(n.replace(',', '')) for n in re.findall(r'\d[\d,]*', s)]
                if nums:
                    rupee_salaries.append(sum(nums) / len(nums))
        
        avg_stipend_str = f"₹{int(sum(rupee_salaries)/len(rupee_salaries)):,}/mo" if rupee_salaries else "Not Disclosed"
        st.markdown(f"""
        <div class="metric-card">
            <div class="metric-value">{avg_stipend_str}</div>
            <div class="metric-label">Avg Stipend (INR)</div>
        </div>
        """, unsafe_allow_html=True)

    st.markdown("<br>", unsafe_allow_html=True)

    # --- Charts & Graphs Section ---
    if not filtered_df.empty:
        chart_col1, chart_col2 = st.columns(2)

        # 1. Technical Skill Demand (Bar Chart)
        with chart_col1:
            st.subheader("🛠️ Most Demanded Skills")
            skills_df = df_skills[df_skills["InternshipId"].isin(filtered_df["InternshipId"])]
            if not skills_df.empty:
                skill_counts = skills_df["SkillName"].value_counts().reset_index()
                skill_counts.columns = ["Skill", "Count"]
                fig_skills = px.bar(
                    skill_counts.head(10), 
                    x="Count", 
                    y="Skill", 
                    orientation='h',
                    color="Count",
                    color_continuous_scale="Blues",
                    labels={"Count": "Job Mentions", "Skill": "Skill Name"},
                    template="plotly_dark"
                )
                fig_skills.update_layout(yaxis={'categoryorder':'total ascending'}, margin=dict(l=20, r=20, t=10, b=10))
                st.plotly_chart(fig_skills, use_container_width=True)
            else:
                st.info("No skills mapped for the filtered entries.")

        # 2. Remote Work Ratio (Donut Chart)
        with chart_col2:
            st.subheader("💻 Work Mode Distribution")
            remote_counts = filtered_df["RemoteType"].value_counts().reset_index()
            remote_counts.columns = ["Mode", "Count"]
            
            fig_remote = px.pie(
                remote_counts, 
                values="Count", 
                names="Mode", 
                hole=0.4,
                color="Mode",
                color_discrete_map={"Remote": "#10b981", "Hybrid": "#f59e0b", "On-site": "#3b82f6"},
                template="plotly_dark"
            )
            fig_remote.update_layout(margin=dict(l=20, r=20, t=10, b=10))
            st.plotly_chart(fig_remote, use_container_width=True)

        chart_col3, chart_col4 = st.columns(2)

        # 3. Location Mapping / Job Frequency by Location
        with chart_col3:
            st.subheader("📍 Job Openings by Location")
            loc_counts = filtered_df["Location"].value_counts().reset_index()
            loc_counts.columns = ["Location", "Openings"]
            
            fig_loc = px.bar(
                loc_counts.head(8),
                x="Location",
                y="Openings",
                color="Openings",
                color_continuous_scale="Purples",
                template="plotly_dark"
            )
            fig_loc.update_layout(margin=dict(l=20, r=20, t=10, b=10))
            st.plotly_chart(fig_loc, use_container_width=True)

        # 4. Job Category Breakdown
        with chart_col4:
            st.subheader("💼 Recruiter Share (Postings count)")
            comp_counts = filtered_df["CompanyName"].value_counts().reset_index()
            comp_counts.columns = ["CompanyName", "Postings"]
            
            fig_comp = px.pie(
                comp_counts.head(5),
                values="Postings",
                names="CompanyName",
                template="plotly_dark",
                color_discrete_sequence=px.colors.qualitative.Pastel
            )
            fig_comp.update_layout(margin=dict(l=20, r=20, t=10, b=10))
            st.plotly_chart(fig_comp, use_container_width=True)
    else:
        st.warning("⚠️ No records match the active filter criteria. Try clearing search filters.")

    # --- Live Data Grid Browser ---
    st.markdown("<hr>", unsafe_allow_html=True)
    st.subheader("📋 Explore Active Transformed Openings")
    st.markdown("Below is the cleaned database content generated by the ETL processes. Search, sort, or inspect individual entries.")
    
    display_cols = ["Title", "CompanyName", "Location", "Salary", "JobType", "RemoteType", "ExperienceLevel", "PostedDate"]
    st.dataframe(
        filtered_df[display_cols].sort_values(by="PostedDate", ascending=False),
        use_container_width=True
    )
else:
    st.error("Could not load data. Ensure SQLite database is present and contains required tables.")
