package com.voximplant.sdk;

import android.app.Activity;
import android.app.Dialog;
import android.app.DialogFragment;
import android.app.Fragment;
import android.app.FragmentTransaction;
import android.content.Context;
import android.content.DialogInterface;
import android.graphics.Color;
import android.graphics.drawable.ColorDrawable;
import android.hardware.Camera;
import android.os.Bundle;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowManager;
import android.widget.FrameLayout;

import com.zingaya.voximplant.VoxImplantClient;

import org.webrtc.SurfaceViewRenderer;

public class CallDialogFragment extends DialogFragment {

    private static final String TAG = CallDialogFragment.class.getSimpleName();

    private static final String ARG_SIZE = "size";
    private static final String ARG_LOCAL = "islocal";

    private boolean isLocal;
    private int[] ViewSizeParams;

    private SurfaceViewRenderer Render;

    public static CallDialogFragment newInstance(int[] pViewParams, boolean pIsLocal) {
        CallDialogFragment fragment = new CallDialogFragment();
        Bundle bundle = new Bundle();
        bundle.putIntArray(ARG_SIZE, pViewParams);
        bundle.putBoolean(ARG_LOCAL, pIsLocal);

        fragment.setArguments(bundle);
        return fragment;
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // Hide title of the dialog
        setStyle(STYLE_NO_FRAME, 0);

        if(getArguments() != null) {
            ViewSizeParams = getArguments().getIntArray(ARG_SIZE);
            isLocal = getArguments().getBoolean(ARG_LOCAL);
        }
    }

    @Override
    public void onStart() {
        super.onStart();

        Dialog dialog = getDialog();
        if (dialog != null) {
            if(ViewSizeParams[0] >= 0) {
                dialog.getWindow().setLayout(ViewSizeParams[2], ViewSizeParams[3]);
                dialog.getWindow().setBackgroundDrawable(new ColorDrawable(Color.TRANSPARENT));
            }
        }
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        Window window = getDialog().getWindow();

        // set "origin" to top left corner, so to speak
        window.setGravity(Gravity.TOP|Gravity.LEFT);

        // after that, setting values for x and y works "naturally"
        WindowManager.LayoutParams params = window.getAttributes();
        params.x = ViewSizeParams[0];
        params.y = ViewSizeParams[1];
        window.setAttributes(params);

        Context context = getActivity();
        FrameLayout rootView = new FrameLayout(context);
        rootView.setLayoutParams(new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
        this.Render = new SurfaceViewRenderer(context);
        this.Render.setLayoutParams(new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
        rootView.addView(this.Render);

        VoxImplantClient.instance().setCameraResolution(320, 240);
        if (isLocal)
            VoxImplantClient.instance().setLocalPreview(this.Render);
        else
            VoxImplantClient.instance().setRemoteView(this.Render);

        this.Render.setZOrderMediaOverlay(true);
        this.Render.requestLayout();

        return rootView;
    }

    @Override
    public void onViewCreated(final View view, Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        // This is done in a post() since the dialog must be drawn before locating.
        getView().post(new Runnable() {

            @Override
            public void run() {

                Window dialogWindow = getDialog().getWindow();

                // Make the dialog possible to be outside touch
                dialogWindow.setFlags(WindowManager.LayoutParams.FLAG_NOT_TOUCH_MODAL,
                        WindowManager.LayoutParams.FLAG_NOT_TOUCH_MODAL);
                dialogWindow.clearFlags(WindowManager.LayoutParams.FLAG_DIM_BEHIND);

                getView().invalidate();
            }
        });
    }


    @Override
    public void onDismiss(DialogInterface dialogInterface) {
        if (isLocal)
            VoxImplantClient.instance().setLocalPreview(null);
        else
            VoxImplantClient.instance().setRemoteView(null);
        if (this.Render != null) {
            Render.release();
            Render = null;
        }
        super.onDismiss(dialogInterface);
    }

    public static CallDialogFragment showDialog(Activity activity, int[] pViewParams, boolean pIsLocal) {
        FragmentTransaction ft = activity.getFragmentManager().beginTransaction();
        String tag = pIsLocal?"local":"remote";
        Fragment prev = activity.getFragmentManager().findFragmentByTag(tag);
        if (prev != null) {
            ft.remove(prev);
        }
        //ft.addToBackStack(tag);
        // Create and show the dialog.
        CallDialogFragment newFragment = CallDialogFragment.newInstance(pViewParams, pIsLocal);
        newFragment.show(ft, tag);
        return newFragment;
    }

    private int currentCamera = Camera.CameraInfo.CAMERA_FACING_FRONT;

    public void switchCameraClick() {
        if (this.currentCamera == Camera.CameraInfo.CAMERA_FACING_FRONT)
            this.currentCamera = Camera.CameraInfo.CAMERA_FACING_BACK;
        else
            this.currentCamera = Camera.CameraInfo.CAMERA_FACING_FRONT;

        VoxImplantClient.instance().setCamera(this.currentCamera);
    }

}