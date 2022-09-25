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

#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable

constant char TestMemory[] = "TestMemory";

kernel void MemoryCopy( global float* pSrc, global float* pDst, long length )
{
	global float* pEnd;
	
	pEnd = pSrc+length;
	while( pSrc<pEnd )
		*pDst++ = *pSrc++;
}

kernel void LoopAndDoNothing( int iterations )
{
	for( int i=0; i<iterations; i++ )
		;
}

kernel void EmptyKernel( )
{
}

struct IOKernelArgs
{
    long outLong;
    int outInt;
    float outSingle;
};

kernel void ArgIO( int i,
  long l,
  float s,
  global struct IOKernelArgs* pA)
{
	pA->outInt = i;
	pA->outLong = l;
	pA->outSingle = s;
}

kernel void TestReadMemory( global read_only char* pData, long length )
{
	int sum;
	
	for( size_t i=0; i<length; i++ )
		sum += pData[i];
}

kernel void TestWriteMemory( global write_only char* pData, long length )
{
	for( size_t i=0; i<length; i++ )
		pData[i] = 1;
}

kernel void TestReadWriteMemory( global char* pData, long length )
{
	for( size_t i=0; i<length/2; i++ )
	{
		char t;
		t = pData[i];
		pData[i] = pData[length-1-i];
		pData[length-1-i] = t;
	}
}

kernel void TestVectorFloat2(float2 f, global float* pS)
{
	*pS++ = f.s0;
	*pS++ = f.s1;
}