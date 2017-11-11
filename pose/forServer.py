#!/usr/bin/env python
"""
Pose predictions in Python.

Caffe must be available on the Pythonpath for this to work. The methods can
be imported and used directly, or the command line interface can be used. In
the latter case, adjust the log-level to your needs. The maximum image size
for one prediction can be adjusted with the variable _MAX_SIZE so that it
still fits in GPU memory, all larger images are split in sufficiently small
parts.

Authors: Christoph Lassner, based on the MATLAB implementation by Eldar
  Insafutdinov.
"""
# pylint: disable=invalid-name
import glob as _glob
import logging as _logging
import os as _os

import caffe as _caffe
import numpy as _np
import scipy as _scipy
from estimate_pose import estimate_pose

_LOGGER = _logging.getLogger(__name__)


def column(matrix, i):
    return [row[i] for row in matrix]


def getAngle(a, b, c):
    a = _np.array(a)
    b = _np.array(b)
    c = _np.array(c)
    ba = b[0:2] - a[0:2]  # normalization of vectors
    bc = b[0:2] - c[0:2]  # normalization of vectors
    cosine_angle = _np.dot(ba, bc) / (_np.linalg.norm(ba) * _np.linalg.norm(bc))
    angle = _np.arccos(cosine_angle)
    return _np.degrees(angle)


def _npcircle(image, cx, cy, radius, color, transparency=0.0):
    """Draw a circle on an image using only numpy methods."""
    radius = int(radius)
    cx = int(cx)
    cy = int(cy)
    y, x = _np.ogrid[-radius: radius, -radius: radius]
    index = x ** 2 + y ** 2 <= radius ** 2
    image[cy - radius:cy + radius, cx - radius:cx + radius][index] = (
        image[cy - radius:cy + radius, cx - radius:cx + radius][index].astype('float32') * transparency +
        _np.array(color).astype('float32') * (1.0 - transparency)).astype('uint8')


###############################################################################
# Command line interface.
###############################################################################

def predict_pose_from(image_name,
                      out_name=None,
                      scales='1.',
                      visualize=True,
                      folder_image_suffix='.png',
                      use_cpu=False,
                      gpu=0):
    """
    Load an image file, predict the pose and write it out.

    `IMAGE_NAME` may be an image or a directory, for which all images with
    `folder_image_suffix` will be processed.
    """
    model_def = '../../models/deepercut/ResNet-152.prototxt'
    model_bin = '../../models/deepercut/ResNet-152.caffemodel'
    scales = [float(val) for val in scales.split(',')]
    if _os.path.isdir(image_name):
        folder_name = image_name[:]
        _LOGGER.info("Specified image name is a folder. Processing all images "
                     "with suffix %s.", folder_image_suffix)
        images = _glob.glob(_os.path.join(folder_name, '*' + folder_image_suffix))
        process_folder = True
    else:
        images = [image_name]
        process_folder = False
    if use_cpu:
        _caffe.set_mode_cpu()
    else:
        _caffe.set_mode_gpu()
        _caffe.set_device(gpu)
    out_name_provided = out_name
    if process_folder and out_name is not None and not _os.path.exists(out_name):
        _os.mkdir(out_name)
    for image_name in images:
        if out_name_provided is None:
            out_name = image_name + '_pose.npz'
        elif process_folder:
            out_name = _os.path.join(out_name_provided,
                                     _os.path.basename(image_name) + '_pose.npz')
        _LOGGER.info("Predicting the pose on `%s` (saving to `%s`) in best of "
                     "scales %s.", image_name, out_name, scales)
        image = _scipy.misc.imread(image_name)
        if image.ndim == 2:
            _LOGGER.warn("The image is grayscale! This may deteriorate performance!")
            image = _np.dstack((image, image, image))
        else:
            image = image[:, :, ::-1]
        pose = estimate_pose(image, model_def, model_bin, scales)
        _np.savez_compressed(out_name, pose=pose)

        print(pose)
        """
        0 Right ankle
        1 Right knee
        2 Right hip
        3 Left hip
        4 Left knee
        5 Left ankle
        6 Right wrist
        7 Right elbow
        8 Right shoulder
        9 Left shoulder
        10 Left elbow
        11 Left wrist
        12 Neck
        13 Head top
        """

        target = open(out_name + '.txt', 'w')
        left_mid = int(getAngle(column(pose, 9), column(pose, 10), column(pose, 11)))
        left_upper = int(getAngle(column(pose, 12), column(pose, 9), column(pose, 10)))
        right_mid = int(getAngle(column(pose, 8), column(pose, 7), column(pose, 6)))
        right_upper = int(getAngle(column(pose, 12), column(pose, 8), column(pose, 7)))
        right_hip = int(getAngle(column(pose, 8), column(pose, 2), column(pose, 1)))
        left_hip = int(getAngle(column(pose, 9), column(pose, 3), column(pose, 4)))
        right_knee =  int(getAngle(column(pose, 2), column(pose, 1), column(pose, 0)))
        left_knee =  int(getAngle(column(pose, 3), column(pose, 4), column(pose, 5)))

        target.write(str(left_mid) + ":" + str(left_upper) + ":" + str(right_mid) + ":" + str(right_upper) + ":" +str(right_hip) + ":"+ str(left_hip) + ":" + str(right_knee) + ":" + str(left_knee))
        target.close()

        if visualize:
            visim = image[:, :, ::-1].copy()
            colors = [[255, 0, 0], [0, 255, 0], [0, 0, 255], [0, 245, 255], [255, 131, 250], [255, 255, 0],
                      [255, 0, 0], [0, 255, 0], [0, 0, 255], [0, 245, 255], [255, 131, 250], [255, 255, 0],
                      [0, 0, 0], [255, 255, 255]]
            for p_idx in range(14):
                _npcircle(visim,
                          pose[0, p_idx],
                          pose[1, p_idx],
                          8,
                          colors[p_idx],
                          0.0)
            vis_name = out_name + '_vis.png'
            _scipy.misc.imsave(vis_name, visim)


if __name__ == '__main__':
    _logging.basicConfig(level=_logging.INFO)
    # pylint: disable=no-value-for-parameter
    predict_pose_from('./',
                      out_name='./out1',
                      scales='1.',
                      visualize=True,
                      folder_image_suffix='.png',
                      use_cpu=False,
                      gpu=0)
