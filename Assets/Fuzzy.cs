using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Fuzzy
{
	public class Membership
	{
		public string name;
		public string type;
		public float[] values;

		public Membership(string _name, string _type, int Length) {
			values = new float[Length];
			name = _name;
			type = _type;
		}

		public Membership(string template) {
			Regex pattern = new Regex(@"MF[0-9]+='(?<name>\w+)':'(?<type>\w+)',\[(?<values>(-?[0-9]\d*(\.\d+)?\s?)+)");
			Match match = pattern.Match(template);

			name = match.Groups["name"].Value;
			type = match.Groups["type"].Value;
			values = match.Groups["values"].Value.Split(' ')
										.Select(x => Convert.ToSingle(x))
										.ToArray();
		}

	}

	public class InputGroup
	{
		public string name;
		public float[] range;
		public Membership[] mfs;

		public InputGroup() {
			
		}

	}
	public class OutputGroup : InputGroup{}

	public class Rule
	{
		public enum Operations {And = 1, Or};
		public Membership[] inputs;
		public Membership[] outputs;
		public int operation;


		public Rule(string template, InputGroup[] inp, OutputGroup[] outp) {
			Regex pattern = new Regex(@"(?<input>([0-9]+\s?)+), (?<output>([0-9]+\s?)+) \(\d+\) : (?<operation>\d+)");
			Match match = pattern.Match(template);

			int[] inputsArr = match.Groups["input"].Value.Split(' ')
										.Select(x => Convert.ToInt32(x))
										.ToArray();

			int[] outputsArr = match.Groups["output"].Value.Split(' ')
										.Select(x => Convert.ToInt32(x))
										.ToArray();

			inputs = new Membership[inputsArr.Length];
			for(int i = 0; i< inputsArr.Length; i++) {
				inputs[i] = inp[i].mfs[inputsArr[i]-1];
			}

			outputs = new Membership[outputsArr.Length];
			for(int i = 0; i< outputsArr.Length; i++) {
				outputs[i] = outp[i].mfs[outputsArr[i]-1];
			}

			operation = Convert.ToInt32(match.Groups["operation"].Value);
		}

		float trimf(float _value, float[] _points){
			float da = (_points[1] - _points[0]);
			float db = (_points[2] - _points[1]);

			float a = (_value - _points[0])/da;
			float b = (_points[2] - _value)/db;

			a = Double.IsNaN(a) ? 1 : a;
			b = Double.IsNaN(b) ? 1 : b;
			return Math.Max(Math.Min(a,b),0);
		}

		float trapmf(float _value, float[] _points){
			float da = (_points[1] - _points[0]);
			float db = (_points[3] - _points[2]);

			float a = (_value - _points[0])/da;
			float b = (_points[3] - _value)/db;

			a = Double.IsNaN(a) ? 1 : a;
			b = Double.IsNaN(b) ? 1 : b;
			return Math.Max(Math.Min(Math.Min(a,b),1),0);
		}

		float orOp(float _a, float _b){
			return Math.Max(_a,_b);
		}

		float andOp(float _a, float _b){
			return Math.Min(_a,_b);
		}

		float Fuzzify(float inp, Membership mfs) {

			if(mfs.type == "trapmf") return trapmf(inp, mfs.values);
			if(mfs.type == "trimf") return trimf(inp, mfs.values);

			return 0;
		}

		public float Eval(float[] inp){
			float value = Fuzzify(inp[0], inputs[0]);
			float aux;
			for(int i = 1; i < inputs.Length; i++) {
				aux = Fuzzify(inp[i], inputs[i]);
				if(operation == (int)Operations.And) {
					value = andOp(value, aux);
					continue;
				}

				if(operation == (int)Operations.Or) {
					value = orOp(value, aux);
					continue;
				}
			}

			return value;
		}
	}


	public class FuzzyLoader
	{
		string[] fileLines;

		public InputGroup[] inMfs;
		public OutputGroup[] outMfs;
		public Rule[] rules;

		public FuzzyLoader(string _path){
			fileLines = System.IO.File.ReadAllLines(_path);

			int numInputs = fileLines
											.Where(x => Regex.IsMatch(x, @"NumInputs=\d+"))
											.Select(x => Convert.ToInt32(Regex.Match(x, @"NumInputs=(?<match>\d+)").Groups["match"].Value))
											.Single();

			int numOutputs = fileLines
											.Where(x => Regex.IsMatch(x, @"NumOutputs=\d+"))
											.Select(x => Convert.ToInt32(Regex.Match(x, @"NumOutputs=(?<match>\d+)").Groups["match"].Value))
											.Single();

			string[] nameArr = fileLines
											.Where(x => Regex.IsMatch(x, @"Name='\w+'"))
											.Select(x => Regex.Match(x, @"Name='(?<match>\w+)'").Groups["match"].Value)
											.ToArray();

			string[] rangeArr = fileLines
											.Where(x => Regex.IsMatch(x, @"Range=\[(-?[0-9]\d*(\.\d+)?\s?)+"))
											.Select(x => Regex.Match(x, @"Range=\[(?<match>(-?[0-9]\d*(\.\d+)?\s?)+)").Groups["match"].Value)
											.ToArray();

			int[] mfsArr = fileLines
											.Where(x => Regex.IsMatch(x, @"NumMFs=\d+"))
											.Select(x => Convert.ToInt32(Regex.Match(x, @"NumMFs=(?<match>\d+)").Groups["match"].Value))
											.ToArray();

			Membership[] mfs = fileLines
							.Where(x => Regex.IsMatch(x, "^MF"))
							.Select(x => new Membership(x))
							.ToArray();

			inMfs = new InputGroup[numInputs];
			int i;
			for(i=0; i < numInputs; i++) {

				inMfs[i] = new InputGroup();

				inMfs[i].name = nameArr[i+1];

				inMfs[i].range = rangeArr[i].Split(' ')
													.Select(x => Convert.ToSingle(x))
													.ToArray();

				inMfs[i].mfs = new Membership[mfsArr[i]];
				for(int j=0; j < mfsArr[i]; j++){
					inMfs[i].mfs[j] = mfs[j+(i*mfsArr[i])];
				}
			}

			outMfs = new OutputGroup[numOutputs];
			int k = i;
			for(i=0; i < numOutputs; i++) {

				outMfs[i] = new OutputGroup();

				outMfs[i].name = nameArr[k+1];

				outMfs[i].range = rangeArr[k].Split(' ')
													.Select(x => Convert.ToSingle(x))
													.ToArray();

				outMfs[i].mfs = new Membership[mfsArr[k]];
				for(int j=0; j < mfsArr[k]; j++){
					outMfs[i].mfs[j] = mfs[j+(k*mfsArr[k])];
				}

				k++;
			}

			rules = fileLines
							.Where(x => Regex.IsMatch(x, ": [1 2]$"))
							.Select(x => new Rule(x, inMfs, outMfs))
							.ToArray();

			
		}
	}

	public class Fis
	{
		FuzzyLoader f;
		public Fis(string _path){
			f = new FuzzyLoader(_path);
		}

		public float[] Eval(float[] inp) {

			//TODO

			float[] ux = new float[f.outMfs.Length];
			float[] u = new float[f.outMfs.Length];
			float[] values = new float[f.outMfs.Length];

			for(int i=0; i < f.rules.Length; i++){
				float r = f.rules[i].Eval(inp);

				for(int j = 0; j < f.rules[i].outputs.Length; j++){
					for(int k = 0; k < f.rules[i].outputs[j].values.Length; k++){
						ux[j] += f.rules[i].outputs[j].values[k]*r;
						u[j] += r;
					}
				}
			}

			for(int j = 0; j < ux.Length; j++){
					values[j] = ux[j]/u[j];
			}

			return values;
		}
	}

}
