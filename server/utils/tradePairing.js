const Trade = require('../models/Trade');
const PairedTrade = require('../models/PairedTrade');
const moment = require('moment');

const pairTrades = async () => {
  try {
    // Get all unpaired trades, sorted by time
    const trades = await Trade.find().sort({ time: 1 });

    // Group trades by instrument
    const tradesByInstrument = {};
    trades.forEach(trade => {
      if (!tradesByInstrument[trade.instrument]) {
        tradesByInstrument[trade.instrument] = [];
      }
      tradesByInstrument[trade.instrument].push(trade);
    });

    const pairedTrades = [];

    // Process each instrument separately
    for (const instrument in tradesByInstrument) {
      const instrumentTrades = tradesByInstrument[instrument];
      const pairs = await pairInstrumentTrades(instrumentTrades);
      pairedTrades.push(...pairs);
    }

    // Save paired trades to database
    if (pairedTrades.length > 0) {
      await PairedTrade.deleteMany({}); // Clear existing pairs
      await PairedTrade.insertMany(pairedTrades);
    }

    return pairedTrades;
  } catch (error) {
    throw new Error('Error pairing trades: ' + error.message);
  }
};

const pairInstrumentTrades = async (trades) => {
  const pairs = [];
  const stack = [];

  for (const trade of trades) {
    if (trade.entryExit === 'Entry') {
      stack.push(trade);
    } else if (trade.entryExit === 'Exit' && stack.length > 0) {
      // Find matching entry trade (FIFO)
      const entryTrade = stack.shift();

      if (entryTrade) {
        const pair = createTradePair(entryTrade, trade);
        pairs.push(pair);
      }
    }
  }

  return pairs;
};

const createTradePair = (entryTrade, exitTrade) => {
  const entryTime = moment(entryTrade.time);
  const exitTime = moment(exitTrade.time);
  const duration = exitTime.diff(entryTime, 'minutes');

  // Determine trade type and calculate PnL
  let pnl = 0;
  let tradeType = '';

  if (entryTrade.action === 'Buy' && exitTrade.action === 'Sell') {
    // Long trade
    tradeType = 'Long';
    pnl = (exitTrade.price - entryTrade.price) * entryTrade.quantity;
  } else if (entryTrade.action === 'Sell' && exitTrade.action === 'Buy') {
    // Short trade
    tradeType = 'Short';
    pnl = (entryTrade.price - exitTrade.price) * entryTrade.quantity;
  }

  const totalCommission = entryTrade.commission + exitTrade.commission;
  pnl -= totalCommission; // Subtract commissions from PnL

  return {
    instrument: entryTrade.instrument,
    entryTrade: entryTrade._id,
    exitTrade: exitTrade._id,
    entryPrice: entryTrade.price,
    exitPrice: exitTrade.price,
    entryTime: entryTrade.time,
    exitTime: exitTrade.time,
    quantity: entryTrade.quantity,
    pnl: parseFloat(pnl.toFixed(2)),
    duration,
    totalCommission,
    tradeType
  };
};

module.exports = {
  pairTrades,
  createTradePair
};
