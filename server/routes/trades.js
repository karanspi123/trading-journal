const express = require('express');
const Trade = require('../models/Trade');
const PairedTrade = require('../models/PairedTrade');
const { generateTradeSummary } = require('../utils/gptSummary');

const router = express.Router();

// Get all trades
router.get('/', async (req, res) => {
  try {
    const trades = await Trade.find().sort({ time: 1 });
    res.json(trades);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Get all paired trades
router.get('/paired', async (req, res) => {
  try {
    const pairedTrades = await PairedTrade.find()
      .populate('entryTrade')
      .populate('exitTrade')
      .sort({ entryTime: 1 });
    res.json(pairedTrades);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Get chart data for a specific instrument
router.get('/chart/:instrument', async (req, res) => {
  try {
    const { instrument } = req.params;
    const pairedTrades = await PairedTrade.find({ instrument })
      .populate('entryTrade')
      .populate('exitTrade')
      .sort({ entryTime: 1 });

    const chartData = pairedTrades.map(trade => ({
      id: trade._id,
      entryTime: trade.entryTime,
      exitTime: trade.exitTime,
      entryPrice: trade.entryPrice,
      exitPrice: trade.exitPrice,
      pnl: trade.pnl,
      tradeType: trade.tradeType,
      duration: trade.duration
    }));

    res.json(chartData);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Generate AI summary for a trade
router.post('/summary/:id', async (req, res) => {
  try {
    const { id } = req.params;
    const pairedTrade = await PairedTrade.findById(id)
      .populate('entryTrade')
      .populate('exitTrade');

    if (!pairedTrade) {
      return res.status(404).json({ error: 'Trade not found' });
    }

    const summary = await generateTradeSummary(pairedTrade);

    // Update the trade with the summary
    pairedTrade.summary = summary;
    await pairedTrade.save();

    res.json({ summary });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Clear all trades
router.delete('/clear', async (req, res) => {
  try {
    await Trade.deleteMany({});
    await PairedTrade.deleteMany({});
    res.json({ message: 'All trades cleared successfully' });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

module.exports = router;
