import React, { useEffect, useRef, useState } from 'react';
import { createChart } from 'lightweight-charts';
import moment from 'moment';

const TradingChart = ({ trades, selectedInstrument }) => {
  const chartContainerRef = useRef();
  const chartRef = useRef();
  const [chart, setChart] = useState(null);

  useEffect(() => {
    if (chartContainerRef.current) {
      const newChart = createChart(chartContainerRef.current, {
        width: chartContainerRef.current.clientWidth,
        height: 400,
        layout: {
          backgroundColor: '#ffffff',
          textColor: '#333',
        },
        grid: {
          vertLines: {
            color: '#e1e1e1',
          },
          horzLines: {
            color: '#e1e1e1',
          },
        },
        crosshair: {
          mode: 1,
        },
        rightPriceScale: {
          borderColor: '#cccccc',
        },
        timeScale: {
          borderColor: '#cccccc',
          timeVisible: true,
          secondsVisible: false,
        },
      });

      chartRef.current = newChart;
      setChart(newChart);

      return () => {
        if (chartRef.current) {
          chartRef.current.remove();
        }
      };
    }
  }, []);

  useEffect(() => {
    if (chart && trades && trades.length > 0) {
      // Clear existing series
      chart.timeScale().fitContent();

      // Filter trades by selected instrument
      const filteredTrades = selectedInstrument
        ? trades.filter(trade => trade.instrument === selectedInstrument)
        : trades;

      if (filteredTrades.length === 0) return;

      // Create line series for price movement
      const lineSeries = chart.addLineSeries({
        color: '#2196F3',
        lineWidth: 2,
      });

      // Prepare price data
      const priceData = [];
      filteredTrades.forEach(trade => {
        priceData.push({
          time: moment(trade.entryTime).unix(),
          value: trade.entryPrice,
        });
        priceData.push({
          time: moment(trade.exitTime).unix(),
          value: trade.exitPrice,
        });
      });

      // Sort by time and remove duplicates
      const sortedPriceData = priceData
        .sort((a, b) => a.time - b.time)
        .filter((item, index, arr) =>
          index === 0 || item.time !== arr[index - 1].time
        );

      lineSeries.setData(sortedPriceData);

      // Add markers for entry and exit points
      const markers = [];
      filteredTrades.forEach(trade => {
        // Entry marker (green)
        markers.push({
          time: moment(trade.entryTime).unix(),
          position: 'belowBar',
          color: '#4CAF50',
          shape: 'arrowUp',
          text: `Entry: $${trade.entryPrice} (${trade.tradeType})`,
        });

        // Exit marker (red)
        markers.push({
          time: moment(trade.exitTime).unix(),
          position: 'aboveBar',
          color: trade.pnl >= 0 ? '#4CAF50' : '#F44336',
          shape: 'arrowDown',
          text: `Exit: $${trade.exitPrice} (P&L: $${trade.pnl})`,
        });
      });

      lineSeries.setMarkers(markers);
    }
  }, [chart, trades, selectedInstrument]);

  // Handle window resize
  useEffect(() => {
    const handleResize = () => {
      if (chart && chartContainerRef.current) {
        chart.applyOptions({
          width: chartContainerRef.current.clientWidth,
        });
      }
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [chart]);

  return (
    <div className="bg-white p-6 rounded-lg shadow-md mb-6">
      <h2 className="text-xl font-bold mb-4">
        Trading Chart {selectedInstrument && `- ${selectedInstrument}`}
      </h2>
      <div
        ref={chartContainerRef}
        className="w-full h-96 border border-gray-200 rounded"
      />
    </div>
  );
};

export default TradingChart;
