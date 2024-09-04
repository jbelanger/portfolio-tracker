'use client';

import HoldingsList from './components/HoldingsList';
import { useParams } from 'next/navigation';



export default function Home() {
  const { portfolioId } = useParams();

  return (
    <HoldingsList portfolioId={1} />
    );
}
