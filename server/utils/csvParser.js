const csv = require('csv-parser');
const fs = require('fs');
const Trade = require('../models/Trade');
const moment = require('moment');

const parseCsvFile = (filePath) => {
  return new Promise((resolve, reject) => {
    const trades = [];

    fs.createReadStream(filePath)
      .pipe(csv())
      .on('data', (row) => {
        try {
          // Parse the CSV row according to NinjaTrader format
          const trade = {
            instrument: row.Instrument,
            action: row.Action,
            quantity: parseInt(row.Quantity),
            price: parseFloat(row.Price),
            time: moment(row.Time, 'M/D/YYYY h:mm:ss A').toDate(),
            id: row.ID,
            entryExit: row['E/X'],
            position: row.Position,
            orderId: row['Order ID'],
            name: row.Name,
            commission: parseFloat(row.Commission.replace('$', '')) || 0,
            rate: parseFloat(row.Rate) || 1,
            account: row.Account,
            connection: row.Connection
          };

          trades.push(trade);
        } catch (error) {
          console.error('Error parsing row:', row, error);
        }
      })
      .on('end', () => {
        resolve(trades);
      })
      .on('error', (error) => {
        reject(error);
      });
  });
};

const saveTradesToDB = async (trades) => {
  try {
    const savedTrades = await Trade.insertMany(trades);
    return savedTrades;
  } catch (error) {
    throw new Error('Error saving trades to database: ' + error.message);
  }
};

module.exports = {
  parseCsvFile,
  saveTradesToDB
};
