import React, { useState } from 'react';
import moment from 'moment';
import { generateTradeSummary } from '../services/api';
import toast from 'react-hot-toast';

const TradeTable = ({ trades, onInstrumentSelect }) => {
  const [loadingSummary, setLoadingSummary] = useState({});
  const [summaries, setSummaries] = useState({});
  const [expandedRows, setExpandedRows] = useState({});

  const handleGenerateSummary = async (tradeId) => {
    setLoadingSummary(prev => ({ ...prev, [tradeId]: true }));

    try {
      const result = await generateTradeSummary(tradeId);
      setSummaries(prev => ({ ...prev, [tradeId]: result.summary }));
      toast.success('Summary generated successfully');
    } catch (error) {
      console.error('Summary generation error:', error);
      toast.error(error.response?.data?.error || 'Failed to generate summary');
    } finally {
      setLoadingSummary(prev => ({ ...prev, [tradeId]: false }));
    }
  };

  const toggleRowExpansion = (tradeId) => {
    setExpandedRows(prev => ({
      ...prev,
      [tradeId]: !prev[tradeId]
    }));
  };

  const formatDuration = (minutes) => {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
  };

  const getPnLColor = (pnl) => {
    return pnl >= 0 ? 'text-green-600' : 'text-red-600';
  };

  const getUniqueInstruments = () => {
    const instruments = [...new Set(trades.map(trade => trade.instrument))];
    return instruments;
  };

  const calculateTotalPnL = () => {
    return trades.reduce((total, trade) => total + trade.pnl, 0).toFixed(2);
  };

  const getWinRate = () => {
    const winningTrades = trades.filter(trade => trade.pnl > 0).length;
    return trades.length > 0 ? ((winningTrades / trades.length) * 100).toFixed(1) : 0;
  };

  if (!trades || trades.length === 0) {
    return (
      <div className="bg-white p-6 rounded-lg shadow-md">
        <h2 className="text-xl font-bold mb-4">Trade History</h2>
        <p className="text-gray-500">No trades available. Upload a CSV file to get started.</p>
      </div>
    );
  }

  return (
    <div className="bg-white p-6 rounded-lg shadow-md">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-xl font-bold">Trade History</h2>
        <div className="flex space-x-4 text-sm">
          <span className="bg-blue-100 px-3 py-1 rounded">
            Total P&L: <span className={getPnLColor(calculateTotalPnL())}>${calculateTotalPnL()}</span>
          </span>
          <span className="bg-green-100 px-3 py-1 rounded">
            Win Rate: {getWinRate()}%
          </span>
          <span className="bg-gray-100 px-3 py-1 rounded">
            Total Trades: {trades.length}
          </span>
        </div>
      </div>

      {/* Instrument Filter */}
      <div className="mb-4">
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Filter by Instrument:
        </label>
        <select
          onChange={(e) => onInstrumentSelect(e.target.value)}
          className="border border-gray-300 rounded-md px-3 py-2 text-sm"
        >
          <option value="">All Instruments</option>
          {getUniqueInstruments().map(instrument => (
            <option key={instrument} value={instrument}>
              {instrument}
            </option>
          ))}
        </select>
      </div>

      {/* Trade Table */}
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Instrument
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Type
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Entry
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Exit
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Duration
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                P&L
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {trades.map((trade) => (
              <React.Fragment key={trade._id}>
                <tr className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    {trade.instrument}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    <span className={`px-2 py-1 rounded text-xs ${
                      trade.tradeType === 'Long'
                        ? 'bg-green-100 text-green-800'
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {trade.tradeType}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    <div>
                      <div>${trade.entryPrice}</div>
                      <div className="text-xs text-gray-400">
                        {moment(trade.entryTime).format('MM/DD HH:mm')}
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    <div>
                      <div>${trade.exitPrice}</div>
                      <div className="text-xs text-gray-400">
                        {moment(trade.exitTime).format('MM/DD HH:mm')}
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {formatDuration(trade.duration)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm">
                    <span className={`font-medium ${getPnLColor(trade.pnl)}`}>
                      ${trade.pnl}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    <div className="flex space-x-2">
                      <button
                        onClick={() => handleGenerateSummary(trade._id)}
                        disabled={loadingSummary[trade._id]}
                        className="text-blue-600 hover:text-blue-900 disabled:text-gray-400"
                      >
                        {loadingSummary[trade._id] ? 'Generating...' : 'AI Summary'}
                      </button>
                      <button
                        onClick={() => toggleRowExpansion(trade._id)}
                        className="text-gray-600 hover:text-gray-900"
                      >
                        {expandedRows[trade._id] ? '▼' : '▶'}
                      </button>
                    </div>
                  </td>
                </tr>
                {expandedRows[trade._id] && (
                  <tr>
                    <td colSpan="7" className="px-6 py-4 bg-gray-50">
                      <div className="text-sm">
                        <div className="grid grid-cols-2 gap-4 mb-3">
                          <div>
                            <strong>Quantity:</strong> {trade.quantity}
                          </div>
                          <div>
                            <strong>Commission:</strong> ${trade.totalCommission}
                          </div>
                        </div>
                        {summaries[trade._id] && (
                          <div className="mt-3 p-3 bg-blue-50 rounded">
                            <strong className="text-blue-800">AI Analysis:</strong>
                            <p className="mt-2 text-gray-700 whitespace-pre-line">
                              {summaries[trade._id]}
                            </p>
                          </div>
                        )}
                      </div>
                    </td>
                  </tr>
                )}
              </React.Fragment>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default TradeTable;
