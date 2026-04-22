import os
from datetime import datetime, timedelta
import yfinance as yf

# Importăm uneltele interne din celelalte servicii
from services.market_service import fetch_and_process_market_data
from services.agent_service import ai_pipeline, vector_db

# Preluam cheile direct din environment
ALPACA_KEY = os.getenv("ALPACA_API_KEY")
ALPACA_SECRET = os.getenv("ALPACA_SECRET_KEY")

def generate_stock_analysis(ticker: str, portfolio_context: str) -> dict:
    """
    Logica centrala de business (Orchestratorul).
    Extrage date tehnice, stirile, istoricul din RAG si apeleaza agentii.
    """
    ticker = ticker.upper()
    
    # 1. Date Tehnice (Yahoo Finance)
    stock = yf.Ticker(ticker)
    hist = stock.history(period="14d")
    if len(hist) < 2:
        raise ValueError(f"Not enough historical data for ticker {ticker}.")
        
    current_price = hist['Close'].iloc[-1]
    chg_1d = ((current_price - hist['Close'].iloc[-2]) / hist['Close'].iloc[-2]) * 100
    chg_7d = ((current_price - hist['Close'].iloc[-7]) / hist['Close'].iloc[-7]) * 100
    tech_data = f"Price: {current_price:.2f} | 1D Change: {chg_1d:.2f}% | 7D Change: {chg_7d:.2f}% | Volatility Index: High"
    
    # 2. Date Fundamentale Live & RAG Ingestion (Alpaca)
    last_update_dt = datetime.fromisoformat(last_update.replace('Z', '+00:00'))
    today_news = fetch_and_process_market_data(
        ticker=ticker, 
        last_update_date=last_update_dt, 
        api_key=ALPACA_KEY, 
        api_secret=ALPACA_SECRET, 
        vector_db=vector_db
    )
    
    # 3. Extragere Memorie Istorică (ChromaDB Vector Search)
    docs_historical = vector_db.similarity_search(f"{ticker} major news events impact", k=5)
    hist_string = ""
    sources_list = []
    
    for d in docs_historical:
        link = d.metadata.get("url", d.metadata.get("link", "No URL"))
        hist_string += f"- {d.page_content} (Source: {link})\n"
        if link != "No URL" and link not in sources_list:
            sources_list.append(link)
            
    if not hist_string:
        hist_string = "No historical patterns found in vector database."
        
    # 4. Asamblare Context pentru Agenti
    initial_state = {
        "ticker": ticker, 
        "raw_tech": tech_data, 
        "raw_news_today": today_news, 
        "raw_news_history": hist_string,
        "portfolio_context": portfolio_context,
        "debate_round": 0,
        "tech_analysis": "",
        "fundamental_analysis": "",
        "bull_args": "",
        "bear_args": "", 
        "risk_analysis": "",
        "judge_decision": "",
        "final_report": ""
    }
    
    # 5. Executarea Magiei AI (LangGraph)
    result = ai_pipeline.invoke(initial_state)
    
    # Returnăm doar datele necesare pentru a forma raspunsul HTTP
    return {
        "final_report": result['final_report'],
        "sources_used": sources_list
    }