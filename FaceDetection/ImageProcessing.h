#pragma once

#include "Frame.h"
#include "FaceDetection.h"

namespace FaceDetection
{
    public delegate void FrameProcessedEvent( unsigned int width, unsigned int height, unsigned int dataPtr );

	[Windows::Foundation::Metadata::WebHostHidden]
    public ref class ImageProcessing sealed
	{
    public:
		ImageProcessing( Detector^ d );

		void processFrame( unsigned int width, unsigned int height, uintptr_t dataPtr );

		Array<int>^ getMotorVelocities();

		event FrameProcessedEvent^ frameProcessed;

	private:
		float rectArea(Rect r);
		float rectCenterX(Rect r);
		float rectCenterY(Rect r);
		void calcMotorVelocities(Rect face);

	private:
		Detector^ m_Detector;
		Array<int>^ m_MV;
    };
}