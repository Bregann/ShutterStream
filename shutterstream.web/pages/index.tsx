import { Carousel } from '@mantine/carousel';
import { Box, Text } from '@mantine/core';
import Autoplay from 'embla-carousel-autoplay';
import Image from 'next/image';
import { useRef } from 'react';

export default function Home() {
  const autoplay = useRef(Autoplay({ delay: 4000 }));

  return (
    <>
      <Text size={60} weight={500} align='center'>Home</Text>
      <Text size={40} weight={400} align='center' sx={{ paddingTop: 20 }}>10 Latest Photos</Text>
      <Carousel
        maw='60%'
        mx="auto"
        withIndicators
        height={600}
        plugins={[autoplay.current]}
        onMouseEnter={autoplay.current.stop}
        onMouseLeave={autoplay.current.reset}>
        <Carousel.Slide>
          <div style={{ width: '100%', height: '100%', position: 'relative' }}>
            <Image src="/DSC_0093.jpg" fill alt="aaa" />
          </div>
        </Carousel.Slide>
        <Carousel.Slide><Image src="/DSC_0078.jpg" width={200} height={200} alt="aaa"></Image></Carousel.Slide>
        <Carousel.Slide>          <div style={{ width: '100%', height: '100%', position: 'relative' }}>
          <Image src="/DSC_0078.jpg" fill alt="aaa" />
        </div></Carousel.Slide>
        <Carousel.Slide>4</Carousel.Slide>
        <Carousel.Slide>5</Carousel.Slide>
        <Carousel.Slide>6</Carousel.Slide>
        <Carousel.Slide>7</Carousel.Slide>
        <Carousel.Slide>8</Carousel.Slide>
      </Carousel>
    </>
  )
}
