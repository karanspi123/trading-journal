import matplotlib.pyplot as plt
import seaborn as sns
import pandas as pd
import numpy as np
from matplotlib.backends.backend_pdf import PdfPages
from matplotlib.patches import Rectangle, FancyBboxPatch
from matplotlib.gridspec import GridSpec
import textwrap
from datetime import datetime

# Set style
plt.style.use('seaborn-v0_8-darkgrid')
sns.set_palette("husl")

# Create PDF
pdf_filename = 'bridgewater_modern_mercantilism_forecast.pdf'

# Forecast data
forecasts = [
    {"id": 1, "question": "Will global trade volume growth remain below 3% annually through 2027?",
     "timeframe": "2 years", "probability": 75, "confidence": "High", "category": "Trade"},
    {"id": 2, "question": "Will the US retain >50% share of global FX reserves by 2030?",
     "timeframe": "5 years", "probability": 65, "confidence": "Medium", "category": "Currency"},
    {"id": 3, "question": "Will China achieve 70% self-sufficiency in semiconductors by 2030?",
     "timeframe": "5 years", "probability": 25, "confidence": "High", "category": "Technology"},
    {"id": 4, "question": "Will a major economy (>$1T GDP) implement capital controls by 2027?",
     "timeframe": "2 years", "probability": 40, "confidence": "Medium", "category": "Financial"},
    {"id": 5, "question": "Will BRICS+ launch a functional payment system bypassing SWIFT by 2030?",
     "timeframe": "5 years", "probability": 80, "confidence": "High", "category": "Currency"},
    {"id": 6, "question": "Will global public debt exceed 100% of GDP by 2030?",
     "timeframe": "5 years", "probability": 90, "confidence": "High", "category": "Debt"},
    {"id": 7, "question": "Will US-China bilateral trade fall below $400B by 2027?",
     "timeframe": "2 years", "probability": 60, "confidence": "Medium", "category": "Trade"},
    {"id": 8, "question": "Will the EU implement a unified digital euro for cross-border trade by 2030?",
     "timeframe": "5 years", "probability": 70, "confidence": "Medium", "category": "Currency"},
    {"id": 9, "question": "Will reshoring increase US manufacturing employment by >1M jobs by 2030?",
     "timeframe": "5 years", "probability": 55, "confidence": "Medium", "category": "Industrial"},
    {"id": 10, "question": "Will India's share of global exports exceed 5% by 2030?",
     "timeframe": "5 years", "probability": 75, "confidence": "High", "category": "Trade"},
    {"id": 11, "question": "Will AI-driven productivity gains add >1% to US GDP growth by 2027?",
     "timeframe": "2 years", "probability": 35, "confidence": "Medium", "category": "Technology"},
    {"id": 12, "question": "Will a G7 country default on sovereign debt by 2030?",
     "timeframe": "5 years", "probability": 20, "confidence": "Low", "category": "Debt"},
    {"id": 13, "question": "Will renewable energy constitute >40% of global energy mix by 2030?",
     "timeframe": "5 years", "probability": 85, "confidence": "High", "category": "Energy"},
    {"id": 14, "question": "Will the yuan's share of global payments exceed 10% by 2027?",
     "timeframe": "2 years", "probability": 45, "confidence": "Medium", "category": "Currency"},
    {"id": 15, "question": "Will global FDI flows remain below 2019 levels through 2027?",
     "timeframe": "2 years", "probability": 70, "confidence": "High", "category": "Investment"},
    {"id": 16, "question": "Will Mexico surpass China as the US's largest trading partner by 2030?",
     "timeframe": "5 years", "probability": 65, "confidence": "Medium", "category": "Trade"},
    {"id": 17, "question": "Will the US federal deficit exceed 7% of GDP by 2027?",
     "timeframe": "2 years", "probability": 80, "confidence": "High", "category": "Debt"},
    {"id": 18, "question": "Will central bank gold reserves exceed 15% of total reserves by 2030?",
     "timeframe": "5 years", "probability": 60, "confidence": "Medium", "category": "Currency"},
    {"id": 19, "question": "Will global carbon border taxes cover >$1T in trade by 2030?",
     "timeframe": "5 years", "probability": 70, "confidence": "High", "category": "Trade"},
    {"id": 20, "question": "Will South Korea or Japan develop nuclear weapons by 2030?",
     "timeframe": "5 years", "probability": 15, "confidence": "Low", "category": "Geopolitical"},
    {"id": 21, "question": "Will food/agriculture constitute >20% of global trade restrictions by 2027?",
     "timeframe": "2 years", "probability": 85, "confidence": "High", "category": "Trade"},
    {"id": 22, "question": "Will quantum computing break current encryption standards by 2030?",
     "timeframe": "5 years", "probability": 25, "confidence": "Low", "category": "Technology"}
]

# Convert to DataFrame
df = pd.DataFrame(forecasts)

with PdfPages(pdf_filename) as pdf:
    # Page 1: Title Page
    fig = plt.figure(figsize=(8.5, 11))
    ax = fig.add_subplot(111)
    ax.axis('off')

    # Title
    ax.text(0.5, 0.85, 'FORECASTING THE FUTURE',
            fontsize=28, weight='bold', ha='center', va='center',
            color='#1f2937')
    ax.text(0.5, 0.80, 'A Modern Economics Challenge',
            fontsize=20, ha='center', va='center', style='italic',
            color='#374151')

    # Main title
    ax.text(0.5, 0.65, 'The Rise of Modern Mercantilism',
            fontsize=32, weight='bold', ha='center', va='center',
            color='#0f172a')
    ax.text(0.5, 0.60, 'and the Reshaping of the Global Economic Order',
            fontsize=18, ha='center', va='center',
            color='#1f2937')

    # Add visual element
    ax.add_patch(Rectangle((0.1, 0.45), 0.8, 0.003,
                           facecolor='#3b82f6', edgecolor='none'))

    # Key insights box
    box = FancyBboxPatch((0.15, 0.15), 0.7, 0.25,
                         boxstyle="round,pad=0.02",
                         facecolor='#f3f4f6',
                         edgecolor='#d1d5db',
                         linewidth=2)
    ax.add_patch(box)

    ax.text(0.5, 0.35, 'Key Thesis:',
            fontsize=14, weight='bold', ha='center', va='center',
            color='#1f2937')

    thesis_text = ("We are witnessing a shift from efficiency-maximizing globalization\n"
                   "to security-maximizing mercantilism, driven by unsustainable\n"
                   "debt levels and great power competition")
    ax.text(0.5, 0.27, thesis_text,
            fontsize=12, ha='center', va='center',
            color='#374151', linespacing=2)

    # Date
    ax.text(0.5, 0.05, f'Prepared: {datetime.now().strftime("%B %Y")}',
            fontsize=10, ha='center', va='center',
            color='#6b7280')

    pdf.savefig(fig, bbox_inches='tight')
    plt.close()

    # Page 2: Executive Summary Dashboard
    fig = plt.figure(figsize=(8.5, 11))
    gs = GridSpec(4, 2, figure=fig, hspace=0.3, wspace=0.3)

    # Overall probability distribution
    ax1 = fig.add_subplot(gs[0, :])
    prob_bins = [0, 20, 40, 60, 80, 100]
    prob_labels = ['0-20%', '20-40%', '40-60%', '60-80%', '80-100%']
    prob_counts = pd.cut(df['probability'], bins=prob_bins).value_counts().sort_index()

    colors = ['#ef4444', '#f59e0b', '#eab308', '#84cc16', '#22c55e']
    bars = ax1.bar(prob_labels, prob_counts.values, color=colors, alpha=0.8)
    ax1.set_title('Distribution of Forecast Probabilities', fontsize=16, weight='bold', pad=20)
    ax1.set_xlabel('Probability Range', fontsize=12)
    ax1.set_ylabel('Number of Forecasts', fontsize=12)

    # Add value labels on bars
    for bar in bars:
        height = bar.get_height()
        ax1.text(bar.get_x() + bar.get_width() / 2., height + 0.1,
                 f'{int(height)}', ha='center', va='bottom', fontsize=10)

    # Category breakdown
    ax2 = fig.add_subplot(gs[1, 0])
    category_counts = df['category'].value_counts()
    colors = sns.color_palette("husl", len(category_counts))
    wedges, texts, autotexts = ax2.pie(category_counts.values,
                                       labels=category_counts.index,
                                       autopct='%1.0f%%',
                                       colors=colors,
                                       startangle=90)
    ax2.set_title('Forecasts by Category', fontsize=14, weight='bold', pad=20)

    # Confidence levels
    ax3 = fig.add_subplot(gs[1, 1])
    conf_counts = df['confidence'].value_counts()
    conf_colors = {'High': '#22c55e', 'Medium': '#f59e0b', 'Low': '#ef4444'}
    bars = ax3.bar(conf_counts.index, conf_counts.values,
                   color=[conf_colors[x] for x in conf_counts.index], alpha=0.8)
    ax3.set_title('Confidence Level Distribution', fontsize=14, weight='bold', pad=20)
    ax3.set_ylabel('Count', fontsize=12)

    # Key statistics
    ax4 = fig.add_subplot(gs[2:, :])
    ax4.axis('off')

    # Create key metrics box
    metrics_text = f"""
    KEY FORECAST METRICS

    • Average Probability: {df['probability'].mean():.1f}%
    • Highest Confidence Predictions: {len(df[df['confidence'] == 'High'])} out of {len(df)}
    • Most Likely Outcome: Global debt exceeding 100% of GDP (90%)
    • Least Likely Outcome: South Korea/Japan nuclear weapons (15%)
    • 2-Year vs 5-Year Split: {len(df[df['timeframe'] == '2 years'])} vs {len(df[df['timeframe'] == '5 years'])}

    TOP 5 HIGHEST PROBABILITY FORECASTS:
    """

    ax4.text(0.5, 0.9, metrics_text, fontsize=12, ha='center', va='top',
             bbox=dict(boxstyle="round,pad=0.5", facecolor='#f3f4f6', edgecolor='#d1d5db'))

    # Add top 5 forecasts
    top_5 = df.nlargest(5, 'probability')[['question', 'probability']]
    y_pos = 0.5
    for idx, row in top_5.iterrows():
        question_wrapped = textwrap.fill(row['question'], width=60)
        ax4.text(0.1, y_pos, f"{row['probability']}% - {question_wrapped}",
                 fontsize=10, ha='left', va='top')
        y_pos -= 0.12

    pdf.savefig(fig, bbox_inches='tight')
    plt.close()

    # Page 3: Detailed Forecast Heatmap
    fig, ax = plt.subplots(figsize=(8.5, 11))

    # Create a subset for visualization
    df_viz = df[['id', 'probability', 'category', 'timeframe', 'confidence']]
    df_pivot = df.pivot_table(values='probability',
                              index='category',
                              columns='timeframe',
                              aggfunc='mean')

    # Create heatmap
    sns.heatmap(df_pivot, annot=True, fmt='.0f', cmap='RdYlGn',
                center=50, vmin=0, vmax=100,
                cbar_kws={'label': 'Average Probability (%)'})

    ax.set_title('Average Forecast Probability by Category and Timeframe',
                 fontsize=16, weight='bold', pad=20)
    ax.set_xlabel('Timeframe', fontsize=12)
    ax.set_ylabel('Category', fontsize=12)

    plt.tight_layout()
    pdf.savefig(fig, bbox_inches='tight')
    plt.close()

    # Page 4: Scenario Analysis
    fig = plt.figure(figsize=(8.5, 11))
    gs = GridSpec(3, 1, figure=fig, hspace=0.4)

    # Scenario probabilities
    ax1 = fig.add_subplot(gs[0])
    scenarios = ['Base Case:\nManaged Decoupling',
                 'Bull Case:\nTech Breakthrough',
                 'Bear Case:\nFragmentation Spiral']
    probs = [60, 15, 25]
    colors = ['#3b82f6', '#22c55e', '#ef4444']

    bars = ax1.barh(scenarios, probs, color=colors, alpha=0.8)
    ax1.set_xlabel('Probability (%)', fontsize=12)
    ax1.set_title('Scenario Analysis: Probability Distribution', fontsize=16, weight='bold', pad=20)
    ax1.set_xlim(0, 100)

    # Add value labels
    for bar, prob in zip(bars, probs):
        ax1.text(bar.get_width() + 1, bar.get_y() + bar.get_height() / 2,
                 f'{prob}%', ha='left', va='center', fontsize=12, weight='bold')

    # Historical parallels
    ax2 = fig.add_subplot(gs[1])
    periods = ['1930s\nGreat Depression', '1970s\nStagflation', '2020s\nModern Mercantilism']
    trade_impact = [-66, -15, -20]
    colors = ['#dc2626', '#f59e0b', '#3b82f6']

    bars = ax2.bar(periods, trade_impact, color=colors, alpha=0.8)
    ax2.set_ylabel('Trade Volume Impact (%)', fontsize=12)
    ax2.set_title('Historical Trade Shocks Comparison', fontsize=14, weight='bold', pad=20)
    ax2.axhline(y=0, color='black', linestyle='-', linewidth=0.5)

    # Add value labels
    for bar in bars:
        height = bar.get_height()
        ax2.text(bar.get_x() + bar.get_width() / 2., height - 2,
                 f'{int(height)}%', ha='center', va='top', fontsize=10,
                 color='white', weight='bold')

    # Timeline projection
    ax3 = fig.add_subplot(gs[2])
    years = list(range(2025, 2031))
    debt_gdp = [95, 96, 97, 98, 99, 100]
    trade_growth = [2.5, 2.3, 2.1, 2.0, 1.8, 1.7]

    ax3_twin = ax3.twinx()

    line1 = ax3.plot(years, debt_gdp, 'o-', color='#dc2626', linewidth=2,
                     markersize=8, label='Global Debt/GDP %')
    line2 = ax3_twin.plot(years, trade_growth, 's-', color='#3b82f6', linewidth=2,
                          markersize=8, label='Trade Growth %')

    ax3.set_xlabel('Year', fontsize=12)
    ax3.set_ylabel('Global Debt/GDP (%)', fontsize=12, color='#dc2626')
    ax3_twin.set_ylabel('Trade Growth (%)', fontsize=12, color='#3b82f6')
    ax3.set_title('Key Metric Projections 2025-2030', fontsize=14, weight='bold', pad=20)

    ax3.tick_params(axis='y', labelcolor='#dc2626')
    ax3_twin.tick_params(axis='y', labelcolor='#3b82f6')

    # Add legend
    lines = line1 + line2
    labels = [l.get_label() for l in lines]
    ax3.legend(lines, labels, loc='center right')

    ax3.grid(True, alpha=0.3)

    pdf.savefig(fig, bbox_inches='tight')
    plt.close()

    # Page 5: Winners and Losers Analysis
    fig = plt.figure(figsize=(8.5, 11))
    ax = fig.add_subplot(111)
    ax.axis('off')

    # Title
    ax.text(0.5, 0.95, 'Winners & Losers in a Mercantilist World',
            fontsize=20, weight='bold', ha='center', va='center')

    # Winners section
    winners_box = FancyBboxPatch((0.05, 0.5), 0.4, 0.4,
                                 boxstyle="round,pad=0.02",
                                 facecolor='#dcfce7',
                                 edgecolor='#22c55e',
                                 linewidth=3)
    ax.add_patch(winners_box)

    ax.text(0.25, 0.85, 'WINNERS', fontsize=16, weight='bold',
            ha='center', va='center', color='#166534')

    winners = [
        "• Commodity producers",
        "  (pricing power in fragmented world)",
        "• Defense/security companies",
        "  (expanding dual-use definition)",
        "• Domestic champions",
        "  (subsidies + protection)",
        "• Financial infrastructure",
        "  (payment systems, exchanges)"
    ]

    y_pos = 0.75
    for winner in winners:
        ax.text(0.07, y_pos, winner, fontsize=11, ha='left', va='top',
                color='#166534')
        y_pos -= 0.06

    # Losers section
    losers_box = FancyBboxPatch((0.55, 0.5), 0.4, 0.4,
                                boxstyle="round,pad=0.02",
                                facecolor='#fee2e2',
                                edgecolor='#ef4444',
                                linewidth=3)
    ax.add_patch(losers_box)

    ax.text(0.75, 0.85, 'LOSERS', fontsize=16, weight='bold',
            ha='center', va='center', color='#991b1b')

    losers = [
        "• Multinational corporations",
        "  (complexity costs rise)",
        "• Unaligned emerging markets",
        "  (squeezed between blocs)",
        "• Global banks",
        "  (compliance nightmares)",
        "• Consumers",
        "  (higher prices, less choice)"
    ]

    y_pos = 0.75
    for loser in losers:
        ax.text(0.57, y_pos, loser, fontsize=11, ha='left', va='top',
                color='#991b1b')
        y_pos -= 0.06

    # Key mechanism diagram
    ax.text(0.5, 0.4, 'Reinforcing Feedback Loops',
            fontsize=16, weight='bold', ha='center', va='center')

    # Create flow diagram
    flow_text = """
    Debt Pressures → Financial Repression → Capital Controls
         ↓                                      ↓
    Trade Deficits ← Industrial Policy ← Strategic Competition
         ↓                                      ↓
    Currency Blocs → Payment Fragmentation → Reduced $ Dominance
    """

    ax.text(0.5, 0.2, flow_text, fontsize=10, ha='center', va='center',
            bbox=dict(boxstyle="round,pad=0.5", facecolor='#f3f4f6',
                      edgecolor='#6b7280', linewidth=2),
            family='monospace')

    pdf.savefig(fig, bbox_inches='tight')
    plt.close()

    # Page 6: Risk Monitoring Dashboard
    fig = plt.figure(figsize=(8.5, 11))
    gs = GridSpec(3, 2, figure=fig, hspace=0.3, wspace=0.3)

    # Leading indicators
    ax1 = fig.add_subplot(gs[0, 0])
    indicators = ['Hedging\nCosts', 'M&A\nApprovals', 'Patent\nPatterns', 'Inventory\nBuilds']
    values = [85, 45, 70, 90]
    colors = ['#ef4444' if v > 70 else '#f59e0b' if v > 50 else '#22c55e' for v in values]

    bars = ax1.bar(indicators, values, color=colors, alpha=0.8)
    ax1.set_ylim(0, 100)
    ax1.set_ylabel('Risk Level', fontsize=10)
    ax1.set_title('Leading Indicators', fontsize=12, weight='bold')
    ax1.axhline(y=70, color='red', linestyle='--', alpha=0.5, linewidth=1)
    ax1.axhline(y=50, color='orange', linestyle='--', alpha=0.5, linewidth=1)

    # Trade restrictions trend
    ax2 = fig.add_subplot(gs[0, 1])
    years = [2015, 2017, 2019, 2021, 2023]
    restrictions = [600, 900, 1200, 1800, 3000]
    ax2.plot(years, restrictions, 'o-', color='#dc2626', linewidth=3, markersize=8)
    ax2.fill_between(years, restrictions, alpha=0.3, color='#dc2626')
    ax2.set_title('Trade Restrictions Growth', fontsize=12, weight='bold')
    ax2.set_xlabel('Year', fontsize=10)
    ax2.set_ylabel('Annual Restrictions', fontsize=10)
    ax2.grid(True, alpha=0.3)

    # Dollar dominance metrics
    ax3 = fig.add_subplot(gs[1, :])
    metrics = ['FX Reserves', 'Trade Invoicing', 'Bond Markets', 'SWIFT Payments']
    current = [58, 50, 65, 42]
    peak = [67, 55, 70, 45]

    x = np.arange(len(metrics))
    width = 0.35

    bars1 = ax3.bar(x - width / 2, peak, width, label='Peak (2001-2010)',
                    color='#3b82f6', alpha=0.6)
    bars2 = ax3.bar(x + width / 2, current, width, label='Current (2024)',
                    color='#1e40af', alpha=0.8)

    ax3.set_ylabel('USD Share (%)', fontsize=10)
    ax3.set_title('Dollar Dominance Erosion', fontsize=12, weight='bold')
    ax3.set_xticks(x)
    ax3.set_xticklabels(metrics, fontsize=9)
    ax3.legend()
    ax3.set_ylim(0, 80)

    # Debt trajectory
    ax4 = fig.add_subplot(gs[2, :])
    countries = ['Global', 'USA', 'China', 'EU', 'Japan', 'Emerging']
    debt_2024 = [93, 124, 83, 85, 255, 65]
    debt_2030 = [100, 140, 95, 92, 270, 75]

    x = np.arange(len(countries))
    width = 0.35

    bars1 = ax4.bar(x - width / 2, debt_2024, width, label='2024',
                    color='#6366f1', alpha=0.8)
    bars2 = ax4.bar(x + width / 2, debt_2030, width, label='2030 (Projected)',
                    color='#dc2626', alpha=0.8)

    ax4.set_ylabel('Debt/GDP (%)', fontsize=10)
    ax4.set_title('Public Debt Projections', fontsize=12, weight='bold')
    ax4.set_xticks(x)
    ax4.set_xticklabels(countries, fontsize=9)
    ax4.legend()
    ax4.axhline(y=100, color='black', linestyle='--', alpha=0.5, linewidth=1)

    # Add danger zone
    ax4.text(5.5, 105, 'Danger Zone', fontsize=8, ha='center',
             color='red', weight='bold')

    pdf.savefig(fig, bbox_inches='tight')
    plt.close()

print(f"PDF generated successfully: {pdf_filename}")

# Create a summary statistics file
summary_stats = f"""
BRIDGEWATER MODERN MERCANTILISM FORECAST - SUMMARY STATISTICS

Total Forecasts: {len(df)}
Average Probability: {df['probability'].mean():.1f}%
Median Probability: {df['probability'].median():.1f}%

By Timeframe:
- 2-year forecasts: {len(df[df['timeframe'] == '2 years'])}
- 5-year forecasts: {len(df[df['timeframe'] == '5 years'])}

By Confidence:
- High confidence: {len(df[df['confidence'] == 'High'])}
- Medium confidence: {len(df[df['confidence'] == 'Medium'])}
- Low confidence: {len(df[df['confidence'] == 'Low'])}

Top Categories:
{df['category'].value_counts().to_string()}

Highest Probability Events:
{df.nlargest(3, 'probability')[['question', 'probability']].to_string(index=False)}

Lowest Probability Events:
{df.nsmallest(3, 'probability')[['question', 'probability']].to_string(index=False)}
"""

# Save summary stats
with open('forecast_summary.txt', 'w') as f:
    f.write(summary_stats)

print("Summary statistics saved to: forecast_summary.txt")