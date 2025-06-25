using System;
using UnityEngine;

namespace Network.Models
{
  [Serializable]
  public class ImageRequest
  {
    public string prompt;
    public int width = 64;
    public int height = 64;
    public int num_images = 1;
  }

  [Serializable]
  public class ImageResponse
  {
    public string base64_image;
  }

  [Serializable]
  public class ReactionRequest
  {
    public string location;
    public string human;
    public string boat;
    public string fish;
    public string size;
  }

  [Serializable]
  public class ReactionResponse
  {
    public string reaction;
  }
}