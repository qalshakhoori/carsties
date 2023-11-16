import React from 'react';

export default function Details({ params }: { params: { id: string } }) {
  return <div className=''>Details of auction {params.id}</div>;
}
