const mongoose = require('mongoose');

const pairedTradeSchema = new mongoose.Schema({
  instrument: {
    type: String,
    required: true
  },
  entryTrade: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Trade',
    required: true
  },
  exitTrade: {
    type: mongoose.Schema.Types.ObjectId,
    ref: 'Trade',
    required: true
  },
  entryPrice: {
    type: Number,
    required: true
  },
  exitPrice: {
    type: Number,
    required: true
  },
  entryTime: {
    type: Date,
    required: true
  },
  exitTime: {
    type: Date,
    required: true
  },
  quantity: {
    type: Number,
    required: true
  },
  pnl: {
    type: Number,
    required: true
  },
  duration: {
    type: Number, // in minutes
    required: true
  },
  totalCommission: {
    type: Number,
    default: 0
  },
  tradeType: {
    type: String,
    enum: ['Long', 'Short'],
    required: true
  },
  summary: {
    type: String,
    default: ''
  }
}, {
  timestamps: true
});

module.exports = mongoose.model('PairedTrade', pairedTradeSchema);
