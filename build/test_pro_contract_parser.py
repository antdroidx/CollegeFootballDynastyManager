import importlib.util,unittest
from pathlib import Path
from bs4 import BeautifulSoup
spec=importlib.util.spec_from_file_location("parser",Path(__file__).with_name("parse-pro-contract-pages.py"));parser=importlib.util.module_from_spec(spec);spec.loader.exec_module(parser)
class ContractHeaders(unittest.TestCase):
 def test_duplicate_css_does_not_move_signing_bonus_into_roster_cash(self):
  html='<table><thead><tr><th>Year</th><th>Base Salary</th><th class="prorated-bonus" colspan="2">Prorated Bonus</th><th class="prorated-bonus" colspan="2">Roster Bonus</th><th>Cap<br>Number</th></tr><tr><th>Signing</th><th>Option</th><th>Regular</th><th>Per Game</th></tr></thead><tbody><tr><td>2026</td><td>$1,000,000</td><td>$2,000,000</td><td>$3,000,000</td><td>$40,000</td><td>$50,000</td><td>$6,090,000</td></tr></tbody></table>'
  row=parser.table_rows(BeautifulSoup(html,'html.parser').table)[0]
  self.assertEqual([row[k] for k in ['salary','signingProration','optionProration','rosterCash','perGameCash','cap']],[1000000,2000000,3000000,40000,50000,6090000])
 def test_simple_header_and_missing_cash_are_preserved(self):
  html='<table><thead><tr><th>Year</th><th>Base Salary</th><th>Prorated Signing Bonus</th><th>Guaranteed Salary</th><th>Cap Number</th></tr></thead><tbody><tr><td>2026</td><td>$885,000</td><td>$461,544</td><td>--</td><td>$1,346,544</td></tr></tbody></table>'
  row=parser.table_rows(BeautifulSoup(html,'html.parser').table)[0]
  self.assertEqual(row['signingProration'],461544);self.assertIsNone(row['reportedGuaranteedCash']);self.assertNotIn('cashPaid',row)
 def test_transaction_values_are_exact_observations(self):
  html='<table><thead><tr><th>Year</th><th>Cap Number</th></tr></thead><tbody><tr><td>2026</td><td>$5</td><td class="player-dead-money"><div class="cut">$71,002,325</div><div class="june_1_cut">$43,002,325</div></td></tr></tbody></table>'
  row=parser.table_rows(BeautifulSoup(html,'html.parser').table)[0];self.assertEqual(row['preJune1CutDead'],71002325);self.assertEqual(row['postJune1CutDead'],43002325)
 def test_dated_tooltip_does_not_drop_future_contract_year(self):
  html='<table><thead><tr><th>Year</th><th>Base Salary</th><th>Cap Number</th></tr></thead><tbody><tr><td>2027 <span class="info-outer"><span>note</span><span class="info-inner">August 1: Roster bonus due</span></span></td><td>$1,050,000</td><td>$13,016,045</td></tr></tbody></table>'
  row=parser.table_rows(BeautifulSoup(html,'html.parser').table)[0];self.assertEqual(row['year'],2027);self.assertIn('Roster bonus due',row['datedNotes'])
 def test_negative_dollars_and_invalid_values(self):
  self.assertEqual(parser.dollars('($1,234)'),-1234)
  with self.assertRaises(ValueError):parser.dollars('$1.2m')
if __name__=='__main__':unittest.main()
