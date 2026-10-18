using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D 背景视差脚本。
///
/// 这个脚本的作用不是让背景跟着摄像机完全同步移动，
/// 而是让背景以比摄像机更慢或更快的视觉速度移动，
/// 从而产生远近层次感。
///
/// 常见的使用方式：
/// - 远景：parallaxEffect 较小，例如 0.1；
/// - 中景：parallaxEffect 约为 0.3；
/// - 近景：parallaxEffect 较大，例如 0.7；
/// - 前景：parallaxEffect 接近 1。
///
/// 背景循环需要满足：
/// 1. 背景物体上有 SpriteRenderer；
/// 2. 背景图片左右边缘可以无缝衔接；
/// 3. 背景物体最好放在不参与物理碰撞的层；
/// 4. 每一个需要独立移动速度的背景层都挂一个脚本实例。
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    /// <summary>
    /// 场景中的主摄像机。
    ///
    /// 脚本通过读取摄像机的 x 坐标，
    /// 判断玩家/摄像机向左或向右移动了多少。
    /// </summary>
    private GameObject cam;

    /// <summary>
    /// 当前背景层的视差系数。
    ///
    /// 这个值决定背景跟随摄像机移动的速度。
    ///
    /// 取值理解：
    /// 0：背景完全不随摄像机水平移动，像无限远处；
    /// 0.5：背景移动摄像机移动距离的一半；
    /// 1：背景和摄像机水平移动速度相同，没有视差效果。
    ///
    /// 注意：这个脚本的循环计算也使用这个值，
    /// 因此建议使用 0 到 1 之间的数值。
    /// </summary>
    [SerializeField] private float parallaxEffect;

    /// <summary>
    /// 当前背景图片的“循环基准 x 坐标”。
    ///
    /// 它不是每一帧都直接等于 transform.position.x。
    /// 当背景图向左或向右循环时，
    /// 脚本会把它向前或向后移动一张图片的宽度。
    /// </summary>
    private float xPosition;

    /// <summary>
    /// 当前背景 Sprite 的世界空间宽度。
    ///
    /// 循环时需要知道一张背景图有多宽，
    /// 才能在它离开摄像机范围后，
    /// 把它移动到另一侧并继续重复使用。
    /// </summary>
    private float length;

    private void Start()
    {
        cam = GameObject.Find("Main Camera");

        // 获取当前背景 SpriteRenderer 在世界空间中的宽度。
        //
        // bounds.size.x 会考虑：
        // - 图片像素尺寸；
        // - Pixels Per Unit；
        // - 物体的缩放。
        //
        // 所以这里得到的是实际场景中的世界单位宽度，
        // 比直接读取纹理像素宽度更适合计算循环。
        length = GetComponent<SpriteRenderer>().bounds.size.x;

        // 记录背景物体的初始 x 坐标。
        //
        // 后续 transform.position.x 会根据摄像机移动产生偏移，
        // 而 xPosition 负责记录当前循环段的基准位置。
        xPosition = transform.position.x;
    }

    private void Update()
    {
        // 这个值主要用于判断背景图是否已经完全离开循环区域。
        float distanceMoved = cam.transform.position.x * (1 - parallaxEffect);

        // 计算背景层应该跟随摄像机移动的距离。
        //
        // 比摄像机慢，视觉上就会显得更远。
        float distanceToMove = cam.transform.position.x * parallaxEffect;

        // 更新背景层的世界位置。
        //
        // xPosition 是循环基准位置；
        // distanceToMove 是根据摄像机位置计算出的视差偏移。
        //
        // y 坐标保持不变，
        // 所以该脚本只处理水平视差。
        transform.position = new Vector3(
            xPosition + distanceToMove,
            transform.position.y
        );

        // 当背景层的“未跟随距离”已经超过当前基准位置加一张图的宽度，
        // 说明该背景图片已经落到摄像机循环区域的左侧。
        //
        // 此时把循环基准向右移动一张图片的宽度，
        // 背景就会从右侧重新出现。
        if (distanceMoved > xPosition + length)
        {
            xPosition = xPosition + length;
        }
        // 当背景层的“未跟随距离”已经小于当前基准位置减一张图的宽度，
        // 说明该背景图片已经落到摄像机循环区域的右侧。
        //
        // 此时把循环基准向左移动一张图片的宽度，
        // 背景就会从左侧重新出现。
        else if (distanceMoved < xPosition - length)
        {
            xPosition = xPosition - length;
        }
    }
}
