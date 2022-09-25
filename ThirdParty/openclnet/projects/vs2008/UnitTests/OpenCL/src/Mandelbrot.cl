/*
 * Copyright (c) 2009 Olav Kalgraf(olav.kalgraf@gmail.com)
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */

__constant uint Palette[17][4] = 
{
    { 0x00, 0x00, 0x00, 0xFF },
    { 0x00, 0xA8, 0x76, 0xFF },
    { 0x20, 0x7E, 0x62, 0xFF },
    { 0x00, 0x6D, 0x4C, 0xFF },
    { 0x35, 0xD4, 0xA4, 0xFF },
    { 0x5F, 0xD4, 0xB1, 0xFF },
    { 0x0D, 0x56, 0xA6, 0xFF },
    { 0x27, 0x4F, 0x7D, 0xFF },
    { 0x04, 0x35, 0x6C, 0xFF },
    { 0x41, 0x86, 0xD3, 0xFF },
    { 0x68, 0x9A, 0xD3, 0xFF },
    { 0x4D, 0xDE, 0x00, 0xFF },
    { 0x55, 0xA6, 0x2A, 0xFF },
    { 0x32, 0x90, 0x00, 0xFF },
    { 0x7A, 0xEE, 0x3C, 0xFF },
    { 0x99, 0xEE, 0x6B, 0xFF },
    { 0x99, 0xEE, 0x6B, 0xFF },
/*
    {0x00, 0x00, 0x5a, 0xff,},
    {0x39, 0x8c, 0xdb, 0xff,},
    {0x9b, 0xb9, 0xff, 0xff,},
    {0x4a, 0xae, 0x92, 0xff,},
    {0x4a, 0xae, 0x92, 0xff,},*/
};

#define MAXITER 100
#define PALETTELENGTH 16

__constant   int8 i8_01234567 = (int8)( 0, 1, 2, 3, 4, 5, 6, 7 );
__constant   int8 i8_00000000 = (int8)( 0, 1, 2, 3, 4, 5, 6, 7 );
__constant   int8 i8_11111111 = (int8)( 0, 1, 2, 3, 4, 5, 6, 7 );

__kernel void Mandelbrot( float left, float top, float right, float bottom, int stride, __global uchar4* pOutput )
{
  size_t width = get_global_size(0);
  size_t height = get_global_size(1);
  size_t cx = get_global_id(0);
  size_t cy = get_global_id(1);
  float dx = (right-left)/(float)width;
  float dy = (bottom-top)/(float)height;
  
  float x0 = left+dx*(float)cx;
  float y0 = top+dy*(float)cy;
  float x = 0.0f;
  float y = 0.0f;
  int iteration = 0;
  int max_iteration = MAXITER;
  
  while( x*x-y*y<=(2*2) && iteration<max_iteration )
  {
    float xtemp = x*x-y*y+x0;
    y = 2*x*y+y0;
    x = xtemp;
    iteration++;
  }
  int index;
  index = iteration*PALETTELENGTH/MAXITER;
  //index = iteration%PALETTELENGTH;
  float4 color0 = (float4)(Palette[index][0],Palette[index][1],Palette[index][2],Palette[index][3]);
  float4 color1 = (float4)(Palette[index+1][0],Palette[index+1][1],Palette[index+1][2],Palette[index+1][3]);
  float mixFactor = clamp( (iteration%(MAXITER/PALETTELENGTH))/(float)(MAXITER/PALETTELENGTH), 0.0f, 1.0f);
  float4 mixFactors = (float4)(1.0f-mixFactor, 1.0f-mixFactor, 1.0f-mixFactor, 1.0f);
  float4 mixfc = mix(  color0, color1, mixFactors );
  mixfc = color0*mixFactors+color1*((float4)(1.0f)-mixFactors);
  pOutput[width*cy+cx] = convert_uchar4( mixfc );
}
