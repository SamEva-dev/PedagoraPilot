"""QA39-D read-only reconciler; no database access, no network, no mutations.
Usage: python reconcile.py authgate.csv pedagora.csv invitations.csv > findings.csv
CSV inputs are the exported results of the three supplied SELECTs (matching headers).
"""
import csv
import sys
from collections import defaultdict


def rows(path):
    with open(path, encoding='utf-8-sig', newline='') as fp:
        return list(csv.DictReader(fp))


def truthy(value):
    return str(value).strip().lower() in ('true', 't', '1', 'yes')


def reconcile(auth, local, invitations):
    loc = defaultdict(list)
    for r in local:
        loc[(r['organization_id'].lower(), r['user_id'].lower())].append(r)
    inv = defaultdict(list)
    for r in invitations:
        if r.get('accepted_user_id'):
            inv[(r['organization_id'].lower(), r['accepted_user_id'].lower())].append(r)
    findings = []
    for a in auth:
        if a['authgate_status'].lower() != 'active':
            continue
        key = (a['organization_id'].lower(), a['user_id'].lower())
        matches = loc.get(key, [])
        if any(truthy(m['local_active']) for m in matches):
            continue
        accepted = [i for i in inv.get(key, []) if i['invitation_status'].lower() == 'accepted']
        findings.append({
            'organization_id': a['organization_id'],
            'user_id': a['user_id'],
            'authgate_role': a['authgate_role'],
            'finding': 'ACTIVE_AUTHGATE_LOCAL_INACTIVE' if matches else 'ACTIVE_AUTHGATE_NO_LOCAL_MEMBER',
            'accepted_invitation_count': len(accepted),
            'action': 'MANUAL_REVIEW_ONLY',
        })
    return findings


def main():
    if len(sys.argv) != 4:
        raise SystemExit('Usage: python reconcile.py authgate.csv pedagora.csv invitations.csv')
    fields = ['organization_id', 'user_id', 'authgate_role', 'finding', 'accepted_invitation_count', 'action']
    writer = csv.DictWriter(sys.stdout, fieldnames=fields)
    writer.writeheader()
    for finding in reconcile(rows(sys.argv[1]), rows(sys.argv[2]), rows(sys.argv[3])):
        writer.writerow(finding)


if __name__ == '__main__':
    main()
