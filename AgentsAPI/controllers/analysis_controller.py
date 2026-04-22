from fastapi import APIRouter, HTTPException
from models.schemas import AnalysisRequest, AnalysisResponse
from services.analysis_service import generate_stock_analysis

router = APIRouter()

@router.post("/analyze", response_model=AnalysisResponse)
async def analyze_stock_endpoint(request: AnalysisRequest):
    """
    Endpoint pentru generarea analizei AI.
    Actioneaza strict ca un router HTTP.
    """
    ticker = request.ticker.upper()
    print(f"\n[API] REQUEST RECEIVED: Analyze {ticker}")
    
    try:
        # Apeleaza logica de business din Service
        result = generate_stock_analysis(ticker, request.portfolio_context)
        
        # Formateaza si returneaza raspunsul
        return AnalysisResponse(
            ticker=ticker,
            final_report=result['final_report'],
            sources_used=result['sources_used']
        )
        
    except ValueError as ve:
        # Erori de business logic aruncate de serviciu (ex: ticker invalid)
        raise HTTPException(status_code=400, detail=str(ve))
    except Exception as e:
        # Erori fatale
        print(f"[API] Error during analysis: {e}")
        raise HTTPException(status_code=500, detail="Internal Server Error during AI analysis.")