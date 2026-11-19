import unittest
from reconcile import reconcile


class ReconcileTests(unittest.TestCase):
    def test_missing_member_flagged_without_action(self):
        a = [{'organization_id':'org1','user_id':'u1','authgate_status':'Active','authgate_role':'Student'}]
        found = reconcile(a, [], [])
        self.assertEqual(found[0]['finding'], 'ACTIVE_AUTHGATE_NO_LOCAL_MEMBER')
        self.assertEqual(found[0]['action'], 'MANUAL_REVIEW_ONLY')

    def test_existing_member_not_flagged(self):
        a = [{'organization_id':'org1','user_id':'u1','authgate_status':'Active','authgate_role':'Student'}]
        m = [{'organization_id':'org1','user_id':'u1','local_active':'true'}]
        self.assertEqual(reconcile(a, m, []), [])

    def test_inactive_grant_ignored(self):
        a = [{'organization_id':'org1','user_id':'u1','authgate_status':'Revoked','authgate_role':'Student'}]
        self.assertEqual(reconcile(a, [], []), [])


if __name__ == '__main__':
    unittest.main()
