const OpenAI = require('openai');

const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY,
});

const generateTradeSummary = async (pairedTrade) => {
  try {
    if (!process.env.OPENAI_API_KEY) {
      return "OpenAI API key not configured";
    }

    const prompt = `
    Analyze this trading data and provide insights:

    Instrument: ${pairedTrade.instrument}
    Trade Type: ${pairedTrade.tradeType}
    Entry Price: ${pairedTrade.entryPrice}
    Exit Price: ${pairedTrade.exitPrice}
    Quantity: ${pairedTrade.quantity}
    PnL: $${pairedTrade.pnl}
    Duration: ${pairedTrade.duration} minutes
    Entry Time: ${pairedTrade.entryTime}
    Exit Time: ${pairedTrade.exitTime}

    Please provide:
    1. Trade performance analysis
    2. Potential mistakes or improvements
    3. Pattern observations
    4. Suggestions for future trades

    Keep the response concise and actionable.
    `;

    const completion = await openai.chat.completions.create({
      model: "gpt-3.5-turbo",
      messages: [
        {
          role: "system",
          content: "You are an expert trading analyst providing constructive feedback on individual trades."
        },
        {
          role: "user",
          content: prompt
        }
      ],
      max_tokens: 300,
      temperature: 0.7,
    });

    return completion.choices[0].message.content;
  } catch (error) {
    console.error('Error generating trade summary:', error);
    return "Error generating summary: " + error.message;
  }
};

module.exports = {
  generateTradeSummary
};
