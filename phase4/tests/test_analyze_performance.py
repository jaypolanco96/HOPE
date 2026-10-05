import csv
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("analyzer", Path(__file__).parents[1] / "analyze_performance.py")
analyzer = importlib.util.module_from_spec(spec); spec.loader.exec_module(analyzer)

class AnalysisTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.path = Path(self.temp.name) / "fixture.csv"
    def tearDown(self): self.temp.cleanup()
    def write(self, rows, measurement="guest_swap_cadence_not_display_fps"):
        with self.path.open("w", newline="", encoding="utf-8") as output:
            output.write(f"# measurement={measurement}\n# fixture=synthetic\n")
            writer = csv.writer(output)
            writer.writerow(["frame", "elapsed_us", "frame_time_us", "gameplay_context", "renderer", "transition"])
            writer.writerows(rows)
    def test_known_distribution(self):
        rows=[]; elapsed=0
        for frame in range(1,1001):
            interval=10000 if frame<=990 else 50000; elapsed+=interval
            rows.append([frame,elapsed,interval,1,"Native",0])
        self.write(rows); result=analyzer.summarize(self.path)["groups"][0]
        self.assertEqual(result["median_ms"],10)
        self.assertEqual(result["p95_ms"],10)
        self.assertEqual(result["p99_ms"],10)
        self.assertEqual(result["worst_ms"],50)
        self.assertAlmostEqual(result["mean_ms"],10.4)
        self.assertAlmostEqual(result["cadence_hz"],1000/10.4)
        self.assertEqual(result["slowest_1_percent_cadence_hz"],20)
        self.assertEqual(result["intervals_over_33_33ms"],10)
        self.assertFalse(result["short_capture"])
    def test_groups_and_transition_exclusion(self):
        self.write([[1,10000,10000,1,"Native",0],[2,30000,20000,0,"Native",1],
                    [3,60000,30000,0,"Native",0],[4,100000,40000,1,"Emulated",0]])
        report=analyzer.summarize(self.path)
        self.assertEqual(report["excluded_transition_intervals"],1)
        self.assertEqual(len(report["groups"]),3)
        self.assertTrue(all(group["samples"]==1 for group in report["groups"]))
    def test_single_sample_and_unknown_context(self):
        self.write([[1,16666,16666,8,"Native",0]])
        group=analyzer.summarize(self.path)["groups"][0]
        self.assertEqual(group["state"],"Context 8")
        self.assertTrue(group["short_capture"])
        self.assertEqual(group["median_ms"],group["p99_ms"])
    def test_empty_and_transition_only(self):
        for rows in ([],[[1,10000,10000,1,"Native",1]]):
            self.write(rows)
            with self.assertRaises(ValueError): analyzer.summarize(self.path)
    def test_invalid_rows(self):
        for bad in ([1,0,0,1,"Native",0],[1,10000,-1,1,"Native",0],
                    [1,10000,"nan",1,"Native",0],[1,10000,10000,-1,"Native",0],
                    [1,10000,10000,1,"Other",0],[1,10000,10000,1,"Native",2],
                    [1,10000,10001,1,"Native",0]):
            with self.subTest(row=bad):
                self.write([bad])
                with self.assertRaises(ValueError): analyzer.summarize(self.path)
    def test_nonmonotonic_sequence(self):
        self.write([[1,10000,10000,1,"Native",0],[1,20000,10000,1,"Native",0]])
        with self.assertRaises(ValueError): analyzer.summarize(self.path)
    def test_reject_aggregate_counter_report(self):
        self.path.write_text("counter,total,avg_per_frame\nframe_time_us,100,10\n")
        with self.assertRaises(ValueError): analyzer.summarize(self.path)
    def test_missing_schema(self):
        self.path.write_text("# measurement=guest_swap_cadence_not_display_fps\nframe,elapsed_us\n1,1000\n")
        with self.assertRaises(ValueError): analyzer.summarize(self.path)
    def test_unsupported_measurement(self):
        self.write([[1,10000,10000,1,"Native",0]],"display_fps")
        with self.assertRaises(ValueError): analyzer.summarize(self.path)

if __name__=="__main__": unittest.main()
