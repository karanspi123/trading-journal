import React, { useState, useEffect } from 'react';
import { Toaster } from 'react-hot-toast';
import FileUpload from './components/FileUpload';
import TradingChart from './components/TradingChart';
import TradeTable from './components/TradeTable';
import { getPairedTrades, clearAllTrades } from './services/api';
import toast from 'react-hot-toast';

function App() {
  const [trades, setTrades] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectedInstrument, setSelectedInstrument] = useState('');

  const fetchTrades = async () => {
    setLoading(true);
    try {
      const pairedTrades = await getPairedTrades();
      setTrades(pairedTrades);
    } catch (error) {
      console.error('Error fetching trades:', error);
      toast.error('Failed to fetch trades');
    } finally {
      setLoading(false);
    }
  };

  const handleUploadSuccess = (result) => {
    setTrades(result.pairedTrades || []);
    setSelectedInstrument('');
  };

  const handleClearTrades = async () => {
    if (window.confirm('Are you sure you want to clear all trades? This action cannot be undone.')) {
      try {
        await clearAllTrades();
        setTrades([]);
        setSelectedInstrument('');
        toast.success('All trades cleared successfully');
      } catch (error) {
        console.error('Error clearing trades:', error);
        toast.error('Failed to clear trades');
      }
    }
  };

  const handleInstrumentSelect = (instrument) => {
    setSelectedInstrument(instrument);
  };

  useEffect(() => {
    fetchTrades();
  }, []);

  return (
    <div className="min-h-screen bg-gray-100">
      <Toaster position="top-right" />

      {/* Header */}
      <header className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">Trading Journal</h1>
              <p className="text-gray-600">NinjaTrader Integration with TradingView Charts</p>
            </div>
            <div className="flex space-x-4">
              <button
                onClick={fetchTrades}
                disabled={loading}
                className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:bg-gray-400"
              >
                {loading ? 'Loading...' : 'Refresh'}
              </button>
              {trades.length > 0 && (
                <button
                  onClick={handleClearTrades}
                  className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700"
                >
                  Clear All
                </button>
              )}
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* File Upload Section */}
        <FileUpload onUploadSuccess={handleUploadSuccess} />

        {/* Loading State */}
        {loading && (
          <div className="text-center py-8">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            <p className="mt-2 text-gray-600">Loading trades...</p>
          </div>
        )}

        {/* Chart and Table Sections */}
        {!loading && trades.length > 0 && (
          <>
            <TradingChart
              trades={trades}
              selectedInstrument={selectedInstrument}
            />
            <TradeTable
              trades={selectedInstrument
                ? trades.filter(trade => trade.instrument === selectedInstrument)
                : trades
              }
              onInstrumentSelect={handleInstrumentSelect}
            />
          </>
        )}

        {/* Empty State */}
        {!loading && trades.length === 0 && (
          <div className="text-center py-12">
            <div className="mx-auto h-12 w-12 text-gray-400">
              <svg fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
            </div>
            <h3 className="mt-2 text-sm font-medium text-gray-900">No trades found</h3>
            <p className="mt-1 text-sm text-gray-500">
              Get started by uploading a CSV file from NinjaTrader.
            </p>
          </div>
        )}
      </main>

      {/* Footer */}
      <footer className="bg-white border-t border-gray-200 mt-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <p className="text-center text-sm text-gray-500">
            Trading Journal - Built with MERN Stack
          </p>
        </div>
      </footer>
    </div>
  );
}

export default App;
