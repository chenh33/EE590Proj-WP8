// ImageProcessingPart2.cpp
#include "ImageProcessing.h"

using namespace FaceDetection;
using namespace Platform;
using namespace Windows::Foundation;


ImageProcessing::ImageProcessing( Detector^ d )
{
	this->m_Detector = d;
	this->m_MV = ref new Array<int>(2);
}

Array<int>^ ImageProcessing::getMotorVelocities()
{
	return m_MV;
}

void ImageProcessing::processFrame( unsigned int width, unsigned int height, uintptr_t dataPtr )
{
	Frame f(width, height, dataPtr, 1);
	Rect face = Rect(0, 0, 0, 0);

	Array<Rect>^ faces = m_Detector->findFaces(width, height, dataPtr, 5, .25f, .2f);
	for( Rect r : faces ) {
		if (rectArea(r) > rectArea(face))
			face = r;
		for( int y = r.Top; y< r.Bottom; ++y ) {
			for( int x = r.Left; x<r.Right; ++x ) {
				// Shade it yellow
				f(x,y).blendIn(0x99ffff00);
			}
		}
	}
	calcMotorVelocities(face);
	frameProcessed(width, height, dataPtr);
}


float ImageProcessing::rectArea(Rect r)
{
	return r.Height*r.Width;
}

float ImageProcessing::rectCenterX(Rect r)
{
	return r.X + 0.5f*r.Width;
}

float ImageProcessing::rectCenterY(Rect r)
{
	return r.Y + 0.5f*r.Height;
}

void ImageProcessing::calcMotorVelocities(Rect face)
{
	if (rectArea(face) == 0) 
	{
		m_MV[0] = 0;
		m_MV[1] = 0;
	}
	else 
	{
		int x_diff = (rectCenterX(face) - 640) * 64 / 640;
		m_MV[0] = 128 - x_diff;
		m_MV[1] = 128 + x_diff;
	}
}

