import os
import re
from dotenv import load_dotenv
from langchain_groq import ChatGroq
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_community.vectorstores import Chroma
from langchain_core.prompts import PromptTemplate
from langchain_core.output_parsers import StrOutputParser
from langgraph.graph import StateGraph, END
from models.schemas import AgentState

load_dotenv()
GROQ_API_KEY = os.getenv("GROQ_API_KEY")

# --- INITIALIZE MODELS ---
llm = ChatGroq(
    temperature=0.0,
    model_name="meta-llama/llama-4-scout-17b-16e-instruct",
    api_key=GROQ_API_KEY
)
embeddings = HuggingFaceEmbeddings(
    model_name="all-MiniLM-L6-v2",
    model_kwargs={'device': 'cpu'},
    encode_kwargs={'batch_size': 256}
    )

vector_db = Chroma(
    embedding_function=embeddings,
    persist_directory="./trading_history_db_backup"
    )

# --- UTILS ---
def extract_block(text: str) -> str:
    match = re.search(r"### OUTPUT(.*?)### END", text, re.S) or re.search(r"### OUTPUT(.*)", text, re.S)
    return match.group(1).strip() if match else text.replace("### OUTPUT", "").strip()

# --- PROMPTS ---
system_rules = """CRITICAL RULE:
Act as a blank slate.
Base your reasoning exclusively on the explicit words and numbers provided in the input text.
Treat the provided text as your only source of truth.
Require explicit textual proof for any relationship between companies or events.
If a piece of information is missing from the text, explicitly state 'Insufficient data'.
Rely solely on provided facts, exact dates, and given percentages."""

tech_prompt = PromptTemplate.from_template(f"""{system_rules}\n
Role: Technical Analyst\n
Data: {{raw_tech}}\n\n

Task:\n
Analyze the provided technical data.\n
Extract and formulate the current trend, support/resistance levels,
and technical momentum using exclusively the numbers provided in the Data section.\n
If a specific metric is missing, explicitly state 'Insufficient data'.\n\n

Strict Format Required:\n
[TREND]: <your analysis of the trend>\n
[SUPPORT/RESISTANCE]: <your analysis of support/resistance>\n
[MOMENTUM]: <your analysis of momentum>\n
### OUTPUT""") | llm | StrOutputParser()

fundamental_prompt = PromptTemplate.from_template(f"""{system_rules}\n
Role: Fundamental Analyst.\n
Today's News: {{raw_news_today}}\n
Historical Patterns: {{raw_news_history}}\n\n

Task:\n
Extract the exact facts from the news.\n
Categorize explicitly negative events (e.g., lawsuits, revenue drops, analyst downgrades) into [BEARISH FACTORS].\n
DO NOT twist positive or neutral news into negative statements just to fill the section.\n
Capture every critical detail, making sure to explicitly include conflicts, market drops,
and risk warnings in the Bearish section.\n
Detail each point thoroughly and specifically using bullet points.\n\n

Strict Format Required:\n
[BULLISH FACTORS]:\n- <positive fact 1>\n
[BEARISH FACTORS]:\n- <negative fact 1>\n
### OUTPUT""") | llm | StrOutputParser()

bull_prompt = PromptTemplate.from_template(f"""{system_rules}\n
Role: BULL Agent.
You advocate for the upward potential of the stock based on current data.\n

Technical Data: {{tech_analysis}}\n
Fundamental Data: {{fundamental_analysis}}\n
Opponent's Bear Argument: {{bear_args}}\n\n

Task:\n
1. REBUTTAL: Evaluate the 'Opponent's Bear Argument'.\n
If the value is "None", write exactly "No previous bear arguments to address."\n
If the value contains text, select one specific claim and counter it using exclusively the [BULLISH FACTORS]
or Technical Data.\n

2. BULL CASE: Construct a data-driven argument explaining why the stock will increase in value.\n
Base your reasoning entirely on the [BULLISH FACTORS] and positive Technical Data.\n
If the [BULLISH FACTORS] section is empty or states insufficient data,
write exactly "Insufficient positive data to build a strong bull case."\n
Provide fresh insights from the data for this specific round.\n
CRITICAL: DO NOT repeat the exact same arguments or cite the exact same news articles from Round 1. Find a new angle.\n\n

Strict Format Required:\n
[REBUTTAL]: <your response>\n
[BULL CASE]: <your argument>\n
### OUTPUT""") | llm | StrOutputParser()

bear_prompt = PromptTemplate.from_template(f"""{system_rules}\n
Role: BEAR Agent.
You advocate for the downside risks and negative catalysts of the stock based on current data.\n

Technical Data: {{tech_analysis}}\n
Fundamental Data: {{fundamental_analysis}}\n
Opponent's Bull Argument: {{bull_args}}\n\n

Task:\n
1. REBUTTAL: Evaluate the 'Opponent's Bull Argument'.\n
If the value is "None", write exactly "No previous bull arguments to address."\n
If the value contains text, select one specific claim and counter it using exclusively the [BEARISH FACTORS]
or Technical Data.\n

2. BEAR CASE: Construct a data-driven argument explaining why the stock will decrease in value.\n
Base your reasoning entirely on the [BEARISH FACTORS] and negative Technical Data.\n
If the [BEARISH FACTORS] section is empty or states insufficient data,
write exactly "Insufficient negative data to build a strong bear case."\n
Provide fresh insights from the data for this specific round.\n
CRITICAL: DO NOT repeat the exact same arguments or cite the exact same news articles from Round 1. Find a new angle.\n\n

Strict Format Required:\n
[REBUTTAL]: <your response>\n
[BEAR CASE]: <your argument>\n
### OUTPUT""") | llm | StrOutputParser()

risk_prompt = PromptTemplate.from_template(f"""{system_rules}\n
Role: Risk Manager.
Evaluate if we should trade {{ticker}} based on the debate AND the user's current portfolio.\n

Portfolio Context:
{{portfolio_context}}\n

Debate Summary:
Bull says: {{bull_args}}
Bear says: {{bear_args}}\n

Task:
1. Assess the trade risk.
2. Exposure Check: Check if the user already has too much of {{ticker}}.
3. Affordability: Check if "Buying Power" is sufficient for a meaningful position.
4. Formulate a mitigation strategy including specific position sizing (e.g., "Invest only 5% of available buying power").\n

Strict Format Required:
[PORTFOLIO RISK]: <analyze if this trade fits the current balance/positions>
[MARKET RISK]: <evaluate the level of risk based on the debate>
[MITIGATION STRATEGY]: <actionable steps including position sizing>
### OUTPUT""") | llm | StrOutputParser()

judge_prompt = PromptTemplate.from_template(f"""{system_rules}\n
Role: Chief Investment Officer (Judge).\n
Bull's Arguments:\n{{bull_args}}\n
Bear's Arguments:\n{{bear_args}}\n
Risk Analysis:\n{{risk_analysis}}\n\n

Task:\nYou are the final judge of this debate.\n
Evaluate the strength of the Bull and Bear cases based STRICTLY on how well they utilized the provided numbers and data
in their [DEBUNK] and [ARGUE] sections.\n
Base your evaluation exclusively on the provided text.\n\n

1. EVALUATE DEBUNK: Identify which agent successfully countered the other's claims using factual data.\n
Reward agents who cite specific provided data.
Penalize agents who invent connections or state unverified claims.\n

2. EVALUATE ARGUMENT: Identify which agent built a stronger case using the provided fundamental and technical data.\n

3. DECISION: State your final decision clearly (BUY, SELL, or HOLD) based on the balance of the arguments
and the Risk Analysis.\n\n

Strict Format Required:\n
[DEBUNK EVALUATION]: <your evaluation>\n
[ARGUMENT EVALUATION]: <your evaluation>\n
[FINAL DECISION]: <BUY/SELL/HOLD> - <brief justification>\n
### OUTPUT""") | llm | StrOutputParser()

mentor_prompt_text = f"""{system_rules}\n
Role: Trading Mentor. \n
CIO Decision: {{judge_decision}}\n
Fundamental Data: {{fundamental_analysis}}\n\n

Task:\nWrite a comprehensive, engaging, and highly digestible educational report for a beginner trader.\n
Translate the logical essence of the CIO Decision into simple, everyday language.\n
DO NOT mention "Bull", "Bear", "debate", "arguments", "agents", or any internal debate processes.
Present this as a unified, expert market analysis.\n
Use very few technical terms. If you MUST use a technical term (like volatility, momentum, trend, etc.),
you must explain it immediately in simple terms using parentheses.\n\n

Format strictly with these sections:\n\n

FINAL VERDICT:\n(Extract the exact decision: BUY, SELL, or HOLD from the CIO Decision)\n\n

MARKET CONTEXT:\n(Explain the current situation simply, using exclusively the provided fundamental and technical text.
Focus on what the company is actually doing)\n\n

THE REASONING:\n(Explain WHY this verdict was reached based on the CIO Decision.
Focus on the facts, numbers, and logic, presenting it as a cohesive strategy rather than a debate outcome)\n\n

TRADING LESSON:\n(Provide specific, actionable advice on how to handle this exact scenario as a beginner,
focusing entirely on risk management, protecting money, and making data-driven decisions)\n\n

### OUTPUT"""
mentor_prompt = PromptTemplate.from_template(mentor_prompt_text) | llm | StrOutputParser()

# --- NODES ---
def node_tech(state):
    print("Running Technical Analyst...")
    return {"tech_analysis": extract_block(tech_prompt.invoke({"raw_tech": state["raw_tech"]}))}
def node_fundamental(state):
    print("Running Fundamental Analyst...")
    return {"fundamental_analysis": extract_block(fundamental_prompt.invoke({"raw_news_today": state["raw_news_today"], "raw_news_history": state["raw_news_history"]}))}
def node_bull(state):
    round_num = state.get("debate_round", 0) + 1
    print(f"Running Bull Agent (Round {round_num})...")
    new_arg = extract_block(bull_prompt.invoke({"tech_analysis": state["tech_analysis"], "fundamental_analysis": state["fundamental_analysis"], "bear_args": state.get("bear_args", "None")}))
    current = state.get("bull_args", "")
    return {"bull_args": f"{current}\n\n--- ROUND {round_num} ---\n{new_arg}".strip()}
def node_bear(state):
    round_num = state.get("debate_round", 0) + 1
    print(f"Running Bear Agent (Round {round_num})...")
    new_arg = extract_block(bear_prompt.invoke({"tech_analysis": state["tech_analysis"], "fundamental_analysis": state["fundamental_analysis"], "bull_args": state.get("bull_args", "None")}))
    current = state.get("bear_args", "")
    return {"bear_args": f"{current}\n\n--- ROUND {round_num} ---\n{new_arg}".strip(), "debate_round": round_num}
def node_risk(state):
    print("Running Risk Manager...")
    return {"risk_analysis": extract_block(risk_prompt.invoke({"bull_args": state["bull_args"], "bear_args": state["bear_args"], "portfolio_context": state["portfolio_context"], "ticker": state["ticker"]}))}
def node_judge(state):
    print("Running Judge...")
    return {"judge_decision": extract_block(judge_prompt.invoke({"bull_args": state["bull_args"], "bear_args": state["bear_args"], "risk_analysis": state["risk_analysis"]}))}
def node_mentor(state):
    print("Running Mentor...")
    return {"final_report": extract_block(mentor_prompt.invoke({"judge_decision": state["judge_decision"], "fundamental_analysis": state["fundamental_analysis"]}))}

def should_continue(state):
    return "continue" if state["debate_round"] < 2 else "end"

# --- COMPILE GRAPH ---
workflow = StateGraph(AgentState)
workflow.add_node("tech", node_tech)
workflow.add_node("fundamental", node_fundamental)
workflow.add_node("bull", node_bull)
workflow.add_node("bear", node_bear)
workflow.add_node("risk", node_risk)
workflow.add_node("judge", node_judge)
workflow.add_node("mentor", node_mentor)

workflow.set_entry_point("tech")
workflow.add_edge("tech", "fundamental")
workflow.add_edge("fundamental", "bull")
workflow.add_edge("bull", "bear")
workflow.add_conditional_edges("bear", should_continue, {"continue": "bull", "end": "risk"})
workflow.add_edge("risk", "judge")
workflow.add_edge("judge", "mentor")
workflow.add_edge("mentor", END)

ai_pipeline = workflow.compile()