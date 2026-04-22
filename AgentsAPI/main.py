from fastapi import FastAPI
import uvicorn
from controllers import analysis_controller
from dotenv import load_dotenv

load_dotenv()

app = FastAPI(title="Trading AI Agent API", description="AI Analyst for Stock Trading via Microservice")

# Conectam controller-ul nostru (asemanator cu app.MapControllers() in C#)
app.include_router(analysis_controller.router)

if __name__ == "__main__":
    print("Starting FastAPI Server...")
    uvicorn.run(app, host="0.0.0.0", port=8000)