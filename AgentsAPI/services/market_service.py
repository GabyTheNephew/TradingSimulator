import re
from datetime import datetime, timedelta
import yfinance as yf
from langchain_core.documents import Document
from alpaca.data.historical.news import NewsClient
from alpaca.data.requests import NewsRequest

def get_company_keyword(ticker: str)->str:
    try:
        stock = yf.Ticker(ticker)
        full_name = stock.info.get('shortName', ticker)
        return full_name.split()[0].replace(',', '')
    except Exception:
        return ticker

def is_article_relevant(headline: str, summary: str, ticker: str,
company_name: str) -> bool:
    content = (headline + " " + summary).lower()
    if ticker.lower() in content or company_name.lower() in content:
        return True
    return False

def fetch_and_process_market_data(ticker: str, last_update_date: datetime,
api_key: str, api_secret: str, vector_db, volatility_threshold=2.0) -> str:
    print(f"\n--- LIVE DATA SYNCHRONIZATION FOR {ticker} ---")
    company_name = get_company_keyword(ticker)
    end_date = datetime.now()

    news_client = NewsClient(api_key, api_secret)
    request_params = NewsRequest(
        symbols=ticker,
        start=last_update_date,
        end=end_date,
        limit=50,
        exclude_countless=True
    )

    stock = yf.Ticker(ticker)
    prices = stock.history(
        start=last_update_date.strftime("%y-%m-%d"),
        end=(end_date + timedelta(days=5)).strftime("%Y-%m-%d")
    )

    if prices.empty:
        return "No price data available to calculate impact."

    prices.index = prices.index.tz_localize(None)
    documents_to_save = []
    recent_news_for_agents = []

    while True:
        try:
            news_batch = news_client.get_news(request_params)
            articles_list = []
            
            if hasattr(news_batch, 'news') and isinstance(news_batch.news, list):
                articles_list = news_batch.news
            elif hasattr(news_batch, 'data') and isinstance(news_batch.data, list):
                articles_list = news_batch.data
                
            if not articles_list: break
                
            for article in articles_list:
                is_dict = isinstance(article, dict)
                headline = article.get('headline', '') if is_dict else getattr(article, 'headline', '')
                summary = article.get('summary', '') if is_dict else getattr(article, 'summary', '')
                
                if not is_article_relevant(headline, summary, ticker, company_name):
                    continue

                created_at = article.get('created_at') if is_dict else getattr(article, 'created_at', None)
                url = article.get('url', 'No URL') if is_dict else getattr(article, 'url', 'No URL')
                
                if not created_at: continue
                    
                news_date = datetime.fromisoformat(created_at.replace('Z', '+00:00')).replace(tzinfo=None).date() if isinstance(created_at, str) else created_at.replace(tzinfo=None).date()
                future_dates = prices.index[prices.index.date >= news_date]
                
                if len(future_dates) >= 4:
                    start_price = prices.loc[future_dates[0]]['Close']
                    end_price = prices.loc[future_dates[3]]['Close'] 
                    impact_pct = ((end_price - start_price) / start_price) * 100
                    
                    if abs(impact_pct) >= volatility_threshold:
                        doc_text = f"EVENT: {headline} - {summary} | MARKET IMPACT: {impact_pct:.2f}% over 3 days."
                        documents_to_save.append(Document(page_content=doc_text, metadata={"date": str(news_date), "impact": float(impact_pct), "url": str(url)}))
                else:
                    recent_news_for_agents.append(f"[{str(news_date)}] {headline}: {summary} (URL: {url})")
            
            next_token = getattr(news_batch, 'next_page_token', None)
            if next_token: request_params.page_token = next_token
            else: break
        except Exception as e:
            print(f"Error fetching news from Alpaca: {e}")
            break
            
    if documents_to_save:
        print(f"Found {len(documents_to_save)} relevant news with confirmed volatility. Adding to RAG...")
        vector_db.add_documents(documents_to_save)
        
    return "\n".join(recent_news_for_agents) if recent_news_for_agents else "No major news in the last few days."