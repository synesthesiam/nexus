using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Nexus.Shared
{
	public class DoubleAnimator
	{
		protected IDictionary<Type, IDictionary<string, PropertyInfo>> typePropertyCache =
			new Dictionary<Type, IDictionary<string, PropertyInfo>>();
			
		protected List<DoubleAnimationInfo> animations = new List<DoubleAnimationInfo>();

		public void PropertyTo(object obj, string propertyName,
		                       double finalValue, double seconds)
		{
			PropertyTo(obj, propertyName, finalValue, seconds, 0, 0, null);
		}
		
		public void PropertyTo(object obj, string propertyName,
		                       double finalValue, double seconds,
		                       int repeatCount, double initialDelay,
		                       Action<DoubleAnimationInfo> afterAction)
		{
			var objectType = obj.GetType();
			
			if (!typePropertyCache.ContainsKey(objectType))
			{
				typePropertyCache[objectType] = new Dictionary<string, PropertyInfo>();
			}
			
			var propertyCache = typePropertyCache[objectType];
			
			if (!propertyCache.ContainsKey(propertyName))
			{
				propertyCache[propertyName] = objectType.GetProperty(propertyName);
			}
			
			var propInfo = propertyCache[propertyName];
			
			// Remove duplicate animations
			animations.RemoveAll(a => (a.Object == obj) && (a.PropertyInfo.Name == propertyName));
			
			double startingValue = (double)propInfo.GetValue(obj, null);
			
			animations.Add(new DoubleAnimationInfo()
			{
				Object = obj,
				PropertyInfo = propInfo,
				Step = (finalValue - startingValue) / seconds,
				CurrentValue = startingValue,
				StartValue = startingValue,
				FinalValue = finalValue,
				Seconds = seconds,
				IsStartLess = (startingValue < finalValue),
				IsFinished = false,
				AfterAction = afterAction,
				RepeatCount = repeatCount,
				InitialDelay = initialDelay
			});
		}
		
		public void Update(double seconds)
		{
			if (animations.Count == 0)
			{
				return;
			}
			
			foreach (var animation in animations)
			{
				double elapsedTime = seconds;
				
				if (animation.InitialDelay > 0)
				{
					animation.InitialDelay -= seconds;

					if (animation.InitialDelay < 0.0)
					{
						elapsedTime = -animation.InitialDelay;
					}
					else
					{
						continue;
					}
				}
				
				animation.CurrentValue += (animation.Step * elapsedTime);
				
				if ((animation.IsStartLess && (animation.CurrentValue >= animation.FinalValue)) ||
					(!animation.IsStartLess && (animation.CurrentValue <= animation.FinalValue)))
				{
					animation.CurrentValue = animation.FinalValue;

					if (animation.RepeatCount <= 0)
					{					
						animation.IsFinished = true;
					}
					else
					{
						animation.RepeatCount--;
						animation.FinalValue = animation.StartValue;
						animation.StartValue = animation.CurrentValue;
						animation.IsStartLess = !animation.IsStartLess;
						animation.Step = -animation.Step;
					}
				}
				
				animation.PropertyInfo.SetValue(animation.Object, animation.CurrentValue, null);
			}

			var finishedAnimations = animations.Where(a => a.IsFinished).ToList();
			
			foreach (var animation in finishedAnimations)
			{
				animations.Remove(animation);

				if (animation.AfterAction != null)
				{
					animation.AfterAction(animation);
				}
			}
		}

		public void FinishAndClear()
		{
			foreach (var animation in animations)
			{
				if ((animation.RepeatCount <= 0) || ((animation.RepeatCount % 2) == 0))
				{
					animation.PropertyInfo.SetValue(animation.Object, animation.FinalValue, null);
				}
				else
				{
					animation.PropertyInfo.SetValue(animation.Object, animation.StartValue, null);
				}
			}

			animations.Clear();
		}
	}
	
	public class DoubleAnimationInfo
	{
		public object Object { get; set; }
		public PropertyInfo PropertyInfo { get; set; }
		public double CurrentValue { get; set; }
		public double StartValue { get; set; }
		public double FinalValue { get; set; }
		public double Step { get; set; }
		public double Seconds { get; set; }
		public bool IsStartLess { get; set; }
		public bool IsFinished { get; set; }
		public Action<DoubleAnimationInfo> AfterAction { get; set; }
		public int RepeatCount { get; set; }
		public double InitialDelay { get; set; }
	}
}
