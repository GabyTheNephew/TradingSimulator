from pydantic import BaseModel
from typing import TypedDict, Optional

class AnalysisRequest(BaseModel):
    ticker: str
    portfolio_context: str
    last_update: str

class AnalysisResponse(BaseModel):
    ticker: str
    final_report: str
    sources_used: list[str]

class AgentState(TypedDict):
    ticker: str
    raw_tech: str
    raw_news_today: str
    raw_news_history: str
    portfolio_context: str
    tech_analysis: Optional[str]
    fundamental_analysis: Optional[str]
    bull_args: Optional[str]
    bear_args: Optional[str]
    debate_round: int
    risk_analysis: Optional[int]
    judge_decision: Optional[str]
    final_report: Optional[str]